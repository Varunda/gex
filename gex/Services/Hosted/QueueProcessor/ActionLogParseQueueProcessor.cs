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

        public ActionLogParseQueueProcessor(ILoggerFactory factory,
            BaseQueue<ActionLogParseQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchProcessingDb processingDb, BaseQueue<GameReplayParseQueueEntry> parseQueue,
            IOptions<FileStorageOptions> options, ActionLogParser actionLogParser,
            GameEventUnitCreatedDb unitCreatedDb, GameEventUnitKilledDb unitKilledDb,
            GameEventUnitDefDb unitDefDb, UnitSetToGameIdDb unitHashDb,
            GameEventTeamStatsDb teamStatsDb)

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
        }

        protected override async Task<bool> _ProcessQueueEntry(ActionLogParseQueueEntry entry, CancellationToken cancel) {

            _Logger.LogInformation($"processing action log [gameID={entry.GameID}]");

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
                await _TeamStatsDb.DeleteByGameID(entry.GameID);
            }

            foreach (GameEventUnitCreated ev in game.Value.UnitsCreated) {
                await _UnitCreatedDb.Insert(ev);
            }

            foreach (GameEventUnitKilled ev in game.Value.UnitsKilled) {
                await _UnitKilledDb.Insert(ev);
            }

            foreach (GameEventTeamStats ev in game.Value.TeamStats) {
                try {
                    await _TeamStatsDb.Insert(ev);
                } catch (Exception ex) {
                    _Logger.LogError($"aaaa");
                }
            }

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

            return true;
        }
    }
}
