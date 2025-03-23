using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Db.Match;
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
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BarReplayDb _ReplayDb;
        private readonly BaseQueue<GameReplayDownloadQueueEntry> _DownloadQueue;

        public GameFetcherPeriodicService(ILoggerFactory loggerFactory, ServiceHealthMonitor healthMon,
            BarReplayApi replayApi, BarMatchRepository matchRepo,
            BarReplayDb replayDb, BaseQueue<GameReplayDownloadQueueEntry> downloadQueue,
            BarMatchProcessingDb processingDb)
        : base("GameFetcherPeriodicService", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

            _ReplayApi = replayApi;
            _MatchRepository = matchRepo;
            _ReplayDb = replayDb;
            _DownloadQueue = downloadQueue;
            _ProcessingDb = processingDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {

            Stopwatch timer = Stopwatch.StartNew();

            int okCount = 0;
            int errorCount = 0;
            int alreadyCount = 0;

            int page = 1;
            int limit = 50;

            while (true) {
                _Logger.LogDebug($"getting recent matches");
                Result<List<BarRecentReplay>, string> recentMatches = await _ReplayApi.GetRecent(page, limit, cancel);
                if (recentMatches.IsOk == false) {
                    return $"error getting recent matches: {recentMatches.Error}";
                }

                _Logger.LogInformation($"found recent matches [count={recentMatches.Value.Count}]");

                if (recentMatches.Value.Count == 0) {
                    return "no matches to get!";
                }

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

                if (okCount == limit && page < 10) {
                    _Logger.LogInformation($"got a full page of new replays, getting another one! [page={page}]");
                    page += 1;
                } else {
                    break;
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

            BarMatch? existingMatch = await _MatchRepository.GetByID(replay.ID, cancel);
            if (existingMatch != null) {
                _Logger.LogTrace($"match already stored in db [ID={replay.ID}]");
                return ParseResult.ALREADY_EXISTS;
            }
            BarMatchProcessing? existingProcessing = await _ProcessingDb.GetByGameID(replay.ID, cancel);
            if (existingProcessing != null) {
                _Logger.LogTrace($"match already being processed (but not in DB yet!) [ID={replay.ID}]");
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
