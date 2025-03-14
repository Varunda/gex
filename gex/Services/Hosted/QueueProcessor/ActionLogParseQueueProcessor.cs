using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class ActionLogParseQueueProcessor : BaseQueueProcessor<ActionLogParseQueueEntry> {

        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly IOptions<FileStorageOptions> _Options;

        private readonly ActionLogParser _ActionLogParser;
        private readonly GameEventUnitCreatedDb _UnitCreatedDb;
        private readonly GameEventUnitKilledDb _UnitKilledDb;
        private readonly GameEventUnitDefDb _UnitDefDb;
        private readonly UnitSetToGameIdDb _UnitHashDb;
        private readonly GameEventTeamStatsDb _TeamStatsDb;
        private readonly GameEventWindUpdateDb _WindUpdateDb;
        private readonly GameEventCommanderPositionUpdateDb _CommanderPositionDb;
        private readonly GameEventUnitTakenDb _UnitTakenDb;
        private readonly GameEventUnitGivenDb _UnitGivenDb;
        private readonly GameEventTransportLoadedDb _TransportLoadedDb;
        private readonly GameEventTransportUnloadedDb _TransportUnloadedDb;
        private readonly GameEventArmyValueUpdateDb _ArmyValueUpdateDb;
        private readonly GameEventFactoryUnitCreatedDb _FactoryCreateDb;

        public ActionLogParseQueueProcessor(ILoggerFactory factory,
            BaseQueue<ActionLogParseQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchProcessingDb processingDb, BaseQueue<GameReplayParseQueueEntry> parseQueue,
            IOptions<FileStorageOptions> options, ActionLogParser actionLogParser,
            GameEventUnitCreatedDb unitCreatedDb, GameEventUnitKilledDb unitKilledDb,
            GameEventUnitDefDb unitDefDb, UnitSetToGameIdDb unitHashDb,
            GameEventTeamStatsDb teamStatsDb, GameEventWindUpdateDb windUpdateDb,
            GameEventCommanderPositionUpdateDb commanderPositionDb, GameEventUnitTakenDb unitTakenDb,
            GameEventUnitGivenDb unitGivenDb, GameEventTransportLoadedDb transportLoaded,
            GameEventTransportUnloadedDb transportUnloaded, GameEventArmyValueUpdateDb armyValueUpdateDb,
            GameEventFactoryUnitCreatedDb factoryCreateDb)

        : base("action_log_parse_queue", factory, queue, serviceHealthMonitor) {

            _ProcessingDb = processingDb;
            _ParseQueue = parseQueue;
            _Options = options;
            _ActionLogParser = actionLogParser;
            _UnitCreatedDb = unitCreatedDb;
            _UnitKilledDb = unitKilledDb;
            _UnitDefDb = unitDefDb;
            _UnitHashDb = unitHashDb;
            _TeamStatsDb = teamStatsDb;
            _WindUpdateDb = windUpdateDb;
            _CommanderPositionDb = commanderPositionDb;
            _UnitTakenDb = unitTakenDb;
            _UnitGivenDb = unitGivenDb;
            _TransportLoadedDb = transportLoaded;
            _TransportUnloadedDb = transportUnloaded;
            _ArmyValueUpdateDb = armyValueUpdateDb;
            _FactoryCreateDb = factoryCreateDb;
        }

        protected override async Task<bool> _ProcessQueueEntry(ActionLogParseQueueEntry entry, CancellationToken cancel) {

            Stopwatch timer = Stopwatch.StartNew();
            _Logger.LogInformation($"processing action log [gameID={entry.GameID}] [force={entry.Force}]");

            string actionLogPath = Path.Join(_Options.Value.GameLogLocation, entry.GameID, "actions.json");
            if (File.Exists(actionLogPath) == false) {
                _Logger.LogError($"failed to find action log [gameID={entry.GameID}] [path={actionLogPath}]");
                return false;
            }

            Result<GameOutput, string> game = await _ActionLogParser.Parse(entry.GameID, actionLogPath, cancel);
            if (game.IsOk == false) {
                _Logger.LogError($"failed to process action log [gameID={entry.GameID}] [error={game.Error}]");
                return false;
            }

            if (entry.Force == true) {
                Stopwatch delTimer = Stopwatch.StartNew();
                _Logger.LogDebug($"deleting old game events [gameID={entry.GameID}]");
                await _ArmyValueUpdateDb.DeleteByGameID(entry.GameID, cancel);
                await _CommanderPositionDb.DeleteByGameID(entry.GameID, cancel);
                await _FactoryCreateDb.DeleteByGameID(entry.GameID, cancel);
                await _TeamStatsDb.DeleteByGameID(entry.GameID, cancel);
                await _TransportLoadedDb.DeleteByGameID(entry.GameID, cancel);
                await _TransportUnloadedDb.DeleteByGameID(entry.GameID, cancel);
                await _UnitCreatedDb.DeleteByGameID(entry.GameID, cancel);
                await _UnitKilledDb.DeleteByGameID(entry.GameID, cancel);
                await _UnitGivenDb.DeleteByGameID(entry.GameID, cancel);
                await _UnitTakenDb.DeleteByGameID(entry.GameID, cancel);
                await _WindUpdateDb.DeleteByGameID(entry.GameID, cancel);
                _Logger.LogInformation($"deleted old game events due to force [gameID={entry.GameID}] [timer={delTimer.ElapsedMilliseconds}ms]");
            }

            await _ArmyValueUpdateDb.InsertMany(game.Value.ArmyValueUpdates, cancel);
            await _CommanderPositionDb.InsertMany(game.Value.CommanderPositionUpdates, cancel);
            await _FactoryCreateDb.InsertMany(game.Value.FactoryUnitCreated, cancel);
            await _TeamStatsDb.InsertMany(game.Value.TeamStats, cancel);
            await _TransportLoadedDb.InsertMany(game.Value.TransportLoaded, cancel);
            await _TransportUnloadedDb.InsertMany(game.Value.TransportUnloaded, cancel);
            await _UnitCreatedDb.InsertMany(game.Value.UnitsCreated, cancel);
            await _UnitKilledDb.InsertMany(game.Value.UnitsKilled, cancel);
            await _UnitGivenDb.InsertMany(game.Value.UnitsGiven, cancel);
            await _UnitTakenDb.InsertMany(game.Value.UnitsTaken, cancel);
            await _WindUpdateDb.InsertMany(game.Value.WindUpdates, cancel);

            if (game.Value.UnitDefinitions.Count > 0) {
                string unitDefHash = game.Value.UnitDefinitions[0].Hash;
                _Logger.LogDebug($"checking if unit def hash exists [gameID={entry.GameID}] [hash={unitDefHash}]");

                List<GameEventUnitDef> unitDefs = await _UnitDefDb.GetByHash(unitDefHash);
                if (unitDefs.Count == 0) {
                    _Logger.LogInformation($"new unit def hash found [hash={unitDefHash}]");

                    foreach (GameEventUnitDef ev in game.Value.UnitDefinitions) {
                        await _UnitDefDb.Insert(ev);
                    }
                } else {
                    _Logger.LogDebug($"unit def already saved [hash={unitDefHash}]");
                }

                await _UnitHashDb.Insert(new GameIdToUnitDefHash() {
                    GameID = entry.GameID,
                    Hash = unitDefHash
                });

            } else {
                _Logger.LogWarning($"missing unit definitions for game! [gameID={entry.GameID}]");
            }

            BarMatchProcessing processing = await _ProcessingDb.GetByGameID(entry.GameID)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.ActionsParsed = DateTime.UtcNow;
            await _ProcessingDb.Upsert(processing);

            _Logger.LogInformation($"parsed action log [gameID={entry.GameID}] [timer={timer.ElapsedMilliseconds}ms]");

            return true;
        }
    }
}
