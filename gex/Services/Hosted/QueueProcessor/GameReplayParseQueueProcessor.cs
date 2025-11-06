using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.Db;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class GameReplayParseQueueProcessor : BaseQueueProcessor<GameReplayParseQueueEntry> {

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarReplayDb _ReplayDb;
        private readonly BarMatchAllyTeamDb _MatchAllyTeamDb;
        private readonly BarMatchSpectatorDb _MatchSpectatorDb;
        private readonly BarMatchChatMessageDb _MatchChatMessageDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMapRepository _BarMapRepository;
        private readonly BarUserRepository _UserRepository;
        private readonly MapPriorityModDb _MapPriorityModDb;
        private readonly BarMatchPriorityCalculator _PriorityCalculator;
        private readonly BarDemofileResultProcessor _ResultProcessor;
        private readonly BarMatchTeamDeathDb _TeamDeathDb;

        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarDemofileParser _Parser;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly BaseQueue<MapStatUpdateQueueEntry> _MapStatUpdateQueue;

        private readonly BaseQueue<UserMapStatUpdateQueueEntry> _UserMapStatUpdateQueue;
        private readonly BaseQueue<UserFactionStatUpdateQueueEntry> _FactionStatUpdateQueue;

        public GameReplayParseQueueProcessor(ILoggerFactory factory,
            BaseQueue<GameReplayParseQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchProcessingRepository processingRepository, IOptions<FileStorageOptions> options,
            BarDemofileParser parser, BaseQueue<HeadlessRunQueueEntry> headlessRunQueue,
            BarMatchRepository matchRepository, BarMatchAllyTeamDb matchAllyTeamDb,
            BarMatchSpectatorDb matchSpectatorDb, BarMatchChatMessageDb matchChatMessageDb,
            BarMatchPlayerRepository playerRepository, BarMapRepository barMapRepository,
            BarReplayDb replayDb, BarUserRepository userRepository,
            BaseQueue<UserMapStatUpdateQueueEntry> userMapStatUpdateQueue,
            BaseQueue<UserFactionStatUpdateQueueEntry> factionStatUpdateQueue, GameVersionUsageDb gameVersionUsageDb,
            MapPriorityModDb mapPriorityModDb, BarMatchPriorityCalculator priorityCalculator,
            BarDemofileResultProcessor resultProcessor, BaseQueue<MapStatUpdateQueueEntry> mapStatUpdateQueue,
            BarMatchTeamDeathDb teamDeathDb) :

        base("game_replay_parse_queue", factory, queue, serviceHealthMonitor) {

            _ProcessingRepository = processingRepository;
            _Options = options;
            _Parser = parser;
            _HeadlessRunQueue = headlessRunQueue;
            _MatchRepository = matchRepository;
            _MatchAllyTeamDb = matchAllyTeamDb;
            _MatchSpectatorDb = matchSpectatorDb;
            _MatchChatMessageDb = matchChatMessageDb;
            _PlayerRepository = playerRepository;
            _BarMapRepository = barMapRepository;
            _ReplayDb = replayDb;
            _UserRepository = userRepository;
            _UserMapStatUpdateQueue = userMapStatUpdateQueue;
            _FactionStatUpdateQueue = factionStatUpdateQueue;
            _MapPriorityModDb = mapPriorityModDb;
            _PriorityCalculator = priorityCalculator;
            _ResultProcessor = resultProcessor;
            _MapStatUpdateQueue = mapStatUpdateQueue;
            _TeamDeathDb = teamDeathDb;
        }

        protected override async Task<bool> _ProcessQueueEntry(GameReplayParseQueueEntry entry, CancellationToken cancel) {

            _Logger.LogInformation($"parsing game replay [gameID={entry.GameID}]");
            Stopwatch timer = Stopwatch.StartNew();

            BarReplay? replay = await _ReplayDb.GetByID(entry.GameID);
            if (replay == null) {
                _Logger.LogError($"cannot parse replay file, replay is missing from DB [gameID={entry.GameID}]");
                return false;
            }

            Stopwatch stepTimer = Stopwatch.StartNew();

            bool runHeadless = entry.ForceForward;

            short priority = -1;

            BarMatch? existingMatch = await _MatchRepository.GetByID(entry.GameID, cancel);
            if (existingMatch != null && entry.Force == false) {
                _Logger.LogInformation($"replay file already parsed [gameID={entry.GameID}]");

                List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(entry.GameID, cancel);
                runHeadless |= players.Count <= 6;
            } else {
                if (entry.Force == true) {
                    Stopwatch delTimer = Stopwatch.StartNew();
                    _Logger.LogInformation($"deleting database data due to forced run [gameID={entry.GameID}]");
                    await _MatchAllyTeamDb.DeleteByGameID(entry.GameID);
                    await _PlayerRepository.DeleteByGameID(entry.GameID);
                    await _MatchSpectatorDb.DeleteByGameID(entry.GameID);
                    await _MatchChatMessageDb.DeleteByGameID(entry.GameID);
                    await _TeamDeathDb.DeleteByGameID(entry.GameID);
                    await _MatchRepository.Delete(entry.GameID);
                    _Logger.LogDebug($"deleting previous game data [gameID={entry.GameID}] [timer={delTimer.ElapsedMilliseconds}ms]");
                }

                string replayPath = Path.Join(_Options.Value.ReplayLocation, replay.FileName);
                if (File.Exists(replayPath) == false) {
                    _Logger.LogError($"missing replay file [gameID={entry.GameID}] [path={replayPath}]");
                    return false;
                }

                FileInfo fi = new(replayPath);
                if (fi.Length > 1024 * 1024 * 64) {
                    _Logger.LogWarning($"demo file is much larger than expected, refusing to parse this [size={fi.Length}] [path={replayPath}]");
                    return false;
                }

                _Logger.LogDebug($"opening game replay [gameID={entry.GameID}] [path={replayPath}]");
                byte[] file = await File.ReadAllBytesAsync(replayPath, cancel);

                Result<BarMatch, string> match = await _Parser.Parse(replay.FileName, file, new DemofileParserOptions(), cancel);
                long parseReplayMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                _Logger.LogDebug($"parsed replay file into match [ID={entry.GameID}]");

                if (match.IsOk == false) {
                    _Logger.LogError($"failed to parse replay data: {match.Error}");
                    return false;
                }

                BarMatch parsed = match.Value;
                parsed.MapName = replay.MapName;

                await _ResultProcessor.Process(parsed, cancel);
                priority = await _PriorityCalculator.Calculate(parsed, cancel);
                runHeadless |= (priority == -1);

                _MapStatUpdateQueue.Queue(new MapStatUpdateQueueEntry() {
                    MapFilename = parsed.MapName
                });
            }

            BarMatchProcessing processing = await _ProcessingRepository.GetByGameID(entry.GameID, cancel)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.Priority = priority;
            processing.ReplayParsed = DateTime.UtcNow;
            processing.ReplayParsedMs = (int)timer.ElapsedMilliseconds;
            await _ProcessingRepository.Upsert(processing);

            if (runHeadless && (entry.ForceForward == true || processing.ReplaySimulated == null)) {
                _Logger.LogDebug($"putting entry into headless run queue [gameID={entry.GameID}]");
                _HeadlessRunQueue.Queue(new HeadlessRunQueueEntry() {
                    GameID = entry.GameID,
                    Force = entry.Force,
                    ForceForward = entry.ForceForward
                });
            }

            return true;
        }

    }
}
