using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Demofile;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class GameFetcherPeriodicService : AppBackgroundPeriodicService {

        private readonly BarReplayApi _ReplayApi;
        private readonly BarDemofileParser _DemofileParser;
        private readonly BarReplayFileRepository _ReplayFileRepository;
        private readonly BarMatchDb _MatchDb;
        private readonly BarMatchAllyTeamDb _MatchAllyTeamDb;
        private readonly BarMatchPlayerDb _MatchPlayerDb;
        private readonly BarMatchSpectatorDb _MatchSpectatorDb;
        private readonly BarMatchChatMessageDb _MatchChatMessageDb;
        private readonly BaseQueue<HeadlessRunQueueEntry> _RunQueue;
        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BarReplayDb _ReplayDb;
        private readonly BaseQueue<GameReplayDownloadQueueEntry> _DownloadQueue;

        public GameFetcherPeriodicService(ILoggerFactory loggerFactory, ServiceHealthMonitor healthMon,
            BarReplayApi replayApi, BarDemofileParser demofileParser,
            BarReplayFileRepository replayFileRepository, BarMatchDb matchDb,
            BarMatchAllyTeamDb matchAllyTeamDb, BarMatchPlayerDb matchPlayerDb,
            BarMatchSpectatorDb matchSpectatorDb, BarMatchChatMessageDb matchChatMessageDb,
            BaseQueue<HeadlessRunQueueEntry> runQueue, BarReplayDb replayDb,
            BaseQueue<GameReplayDownloadQueueEntry> downloadQueue, BarMatchProcessingDb processingDb)
        : base("GameFetcherPeriodicService", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

            _ReplayApi = replayApi;
            _DemofileParser = demofileParser;
            _ReplayFileRepository = replayFileRepository;
            _MatchDb = matchDb;
            _MatchAllyTeamDb = matchAllyTeamDb;
            _MatchPlayerDb = matchPlayerDb;
            _MatchSpectatorDb = matchSpectatorDb;
            _MatchChatMessageDb = matchChatMessageDb;
            _RunQueue = runQueue;
            _ReplayDb = replayDb;
            _DownloadQueue = downloadQueue;
            _ProcessingDb = processingDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {

            Stopwatch timer = Stopwatch.StartNew();
            _Logger.LogDebug($"getting recent matches");
            Result<List<BarRecentReplay>, string> recentMatches = await _ReplayApi.GetRecent(cancel);
            if (recentMatches.IsOk == false) {
                return $"error getting recent matches: {recentMatches.Error}";
            }

            _Logger.LogInformation($"found recent matches [count={recentMatches.Value.Count}]");

            if (recentMatches.Value.Count == 0) {
                return "no matches to get!";
            }

            int okCount = 0;
            int errorCount = 0;
            int alreadyCount = 0;

            foreach (BarRecentReplay replay in recentMatches.Value) {
                try {
                    if (cancel.IsCancellationRequested) {
                        break;
                    }

                    ParseResult result = await ProcessRecentMatch(replay, cancel);
                    if (result == ParseResult.OK) {
                        ++okCount;
                    } else if (result == ParseResult.ALREADY_EXISTS) {
                        ++alreadyCount;
                    } else if (result == ParseResult.ERROR) {
                        ++errorCount;
                    }
                } catch (Exception e) {
                    _Logger.LogError(e, $"failed to process recent match [ID={replay.ID}]");
                    ++errorCount;
                }
            }

            string msg = $"processed batch of replays [duration={timer.ElapsedMilliseconds}ms]"
                + $" [ok={okCount}] [error={errorCount}] [stored={alreadyCount}]";
            _Logger.LogInformation(msg);

            return msg;
        }

        private async Task<ParseResult> ProcessRecentMatch(BarRecentReplay replay, CancellationToken cancel) {
            _Logger.LogDebug($"loading replay [ID={replay.ID}]");
            Stopwatch timer = Stopwatch.StartNew();

            BarMatch? existingMatch = await _MatchDb.GetByID(replay.ID);
            if (existingMatch != null) {
                _Logger.LogDebug($"match already stored in db [ID={replay.ID}]");
                return ParseResult.ALREADY_EXISTS;
            }


            Stopwatch stepTimer = Stopwatch.StartNew();
            Result<BarReplay, string> result = await _ReplayApi.GetReplay(replay.ID, cancel);
            if (result.IsOk == false) {
                _Logger.LogError($"error getting replay info: {result.Error}");
                return ParseResult.ERROR;
            }
            long getReplayInfoMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            BarReplay? existingReplay = await _ReplayDb.GetByID(replay.ID);
            if (existingReplay != null) {
                _Logger.LogDebug($"match replay already saved, but the match doesn't exist, processing further [gameID={replay.ID}]");
            } else {
                await _ReplayDb.Insert(result.Value);
            }

            BarMatchProcessing processing = new();
            processing.GameID = result.Value.ID;

            await _ProcessingDb.Upsert(processing);

            _DownloadQueue.Queue(new GameReplayDownloadQueueEntry() {
                GameID = result.Value.ID
            });

            return ParseResult.OK;

            string replayFileName = result.Value.FileName;
            Result<byte[], string> replayFile = await _ReplayFileRepository.GetReplay(replayFileName, cancel);
            if (replayFile.IsOk == false) {
                _Logger.LogError($"error getting replay data: {replayFile.Error}");
                return ParseResult.ERROR;
            }
            long openReplayFileMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            _Logger.LogDebug($"loaded replay file! [id={replay.ID}] [filename={replayFileName}]");

            Result<BarMatch, string> match = await _DemofileParser.Parse(replayFileName, replayFile.Value, cancel);
            long parseReplayMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            _Logger.LogDebug($"parsed replay file into match [ID={replay.ID}]");

            if (match.IsOk == false) {
                _Logger.LogError($"failed to parse replay data: {match.Error}");
                return ParseResult.ERROR;
            }

            BarMatch parsed = match.Value;

            foreach (BarMatchAllyTeam allyTeam in parsed.AllyTeams) {
                await _MatchAllyTeamDb.Insert(allyTeam);
            }
            long insertAllyTeamsMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchPlayer player in parsed.Players) {
                await _MatchPlayerDb.Insert(player);
            }
            long insertPlayersMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchSpectator spec in parsed.Spectators) {
                await _MatchSpectatorDb.Insert(spec);
            }
            long insertSpecMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchChatMessage msg in parsed.ChatMessages) {
                await _MatchChatMessageDb.Insert(msg);
            }
            long insertChatMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            await _MatchDb.Insert(parsed, cancel);
            long insertMatchMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            _Logger.LogInformation($"processed match [ID={replay.ID}] [duration={timer.ElapsedMilliseconds}ms]" 
                + $" [get replay={getReplayInfoMs}ms] [open replay={openReplayFileMs}ms] [parse replay={parseReplayMs}ms]"
                + $" [ally team db={insertAllyTeamsMs}ms] [player db={insertPlayersMs}ms] [match db={insertMatchMs}ms]");

            if (parsed.Players.Count == 2) {
                _RunQueue.Queue(new Models.Queues.HeadlessRunQueueEntry() {
                    GameID = parsed.ID,
                });
            }

            return ParseResult.OK;
        }

        /// <summary>
        ///     enum that holds the possible results of calling <see cref="ProcessRecentMatch(BarRecentReplay, CancellationToken)"/>
        /// </summary>
        private enum ParseResult {

            /// <summary>
            ///     everything went ok! no issues, match is now stored in DB
            /// </summary>
            OK,

            /// <summary>
            ///     this replay is already in the db, no further work is needed
            /// </summary>
            ALREADY_EXISTS,

            /// <summary>
            ///     an error occured while processing the replay!
            /// </summary>
            ERROR

        }

    }
}
