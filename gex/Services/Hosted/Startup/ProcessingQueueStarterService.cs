using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.Db.Match;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class ProcessingQueueStarterService : IHostedService {

        private readonly ILogger<ProcessingQueueStarterService> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;

        private readonly BaseQueue<GameReplayDownloadQueueEntry> _DownloadQueue;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogParseQueue;

        public ProcessingQueueStarterService(ILogger<ProcessingQueueStarterService> logger,
            BarMatchProcessingDb processingDb, BaseQueue<GameReplayDownloadQueueEntry> downloadQueue,
            BaseQueue<GameReplayParseQueueEntry> parseQueue, BaseQueue<HeadlessRunQueueEntry> headlessRunQueue,
            BaseQueue<ActionLogParseQueueEntry> actionLogParseQueue, BarMatchRepository matchRepository,
            BarMatchPlayerRepository playerRepository) {

            _Logger = logger;
            _ProcessingDb = processingDb;
            _DownloadQueue = downloadQueue;
            _ParseQueue = parseQueue;
            _HeadlessRunQueue = headlessRunQueue;
            _ActionLogParseQueue = actionLogParseQueue;
            _MatchRepository = matchRepository;
            _PlayerRepository = playerRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(async () => {
                await Start(cancellationToken);
            }, cancellationToken);
        }

        private async Task Start(CancellationToken cancel) {

            _Logger.LogInformation($"finding matches being processed and inserting into queues");

            List<BarMatchProcessing> processing = await _ProcessingDb.GetPending();
            _Logger.LogDebug($"loaded matches to be further processed [count={processing.Count}]");

            foreach (BarMatchProcessing proc in processing) {

                _Logger.LogTrace($"figuring out what queue this entry goes into into [gameID={proc.GameID}"
                    + $" [downloaded={proc.ReplayDownloaded:u}] [parsed={proc.ReplayParsed:u}] [simulated={proc.ReplaySimulated:u}] [action={proc.ActionsParsed:u}]");

                if (proc.ReplayDownloaded == null) {
                    _DownloadQueue.Queue(new GameReplayDownloadQueueEntry() { GameID = proc.GameID, Force = true });
                } else if (proc.ReplayParsed == null) {
                    BarMatch? match = await _MatchRepository.GetByID(proc.GameID, cancel);
                    if (match == null) {
                        _Logger.LogInformation($"game is not in the database, cannot get filename, need to download [gameID={proc.GameID}]");
                        _DownloadQueue.Queue(new GameReplayDownloadQueueEntry() { GameID = proc.GameID, Force = true });
                    } else {
                        _ParseQueue.Queue(new GameReplayParseQueueEntry() {
                            GameID = proc.GameID, Force = true
                        });
                    }

                } else if (proc.ReplaySimulated == null) {
                    List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(proc.GameID, cancel);
                    if (players.Count == 2) {
                        _HeadlessRunQueue.Queue(new HeadlessRunQueueEntry() { GameID = proc.GameID, Force = true });
                    } else {
                        _Logger.LogWarning($"why is this game in here? [gameID={proc.GameID}]");
                    }
                } else if (proc.ReplaySimulated != null && proc.ActionsParsed == null) {
                    _ActionLogParseQueue.Queue(new ActionLogParseQueueEntry() { GameID = proc.GameID, Force = true });
                } else {
                    _Logger.LogWarning($"logic check failed, not sure what queue this entry goes into [gameID={proc.GameID}"
                        + $" [downloaded={proc.ReplayDownloaded:u}] [parsed={proc.ReplayParsed:u}] [simulated={proc.ReplaySimulated:u}] [action={proc.ActionsParsed:u}]");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
