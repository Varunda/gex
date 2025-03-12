using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.Db;
using gex.Services.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class ProcessingQueueStarterService : IHostedService {

        private readonly ILogger<ProcessingQueueStarterService> _Logger;
        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BarMatchPlayerDb _MatchPlayerDb;

        private readonly BaseQueue<GameReplayDownloadQueueEntry> _DownloadQueue;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogParseQueue;

        public ProcessingQueueStarterService(ILogger<ProcessingQueueStarterService> logger,
            BarMatchProcessingDb processingDb, BarMatchPlayerDb matchPlayerDb,
            BaseQueue<GameReplayDownloadQueueEntry> downloadQueue, BaseQueue<GameReplayParseQueueEntry> parseQueue,
            BaseQueue<HeadlessRunQueueEntry> headlessRunQueue, BaseQueue<ActionLogParseQueueEntry> actionLogParseQueue) {

            _Logger = logger;
            _ProcessingDb = processingDb;
            _MatchPlayerDb = matchPlayerDb;
            _DownloadQueue = downloadQueue;
            _ParseQueue = parseQueue;
            _HeadlessRunQueue = headlessRunQueue;
            _ActionLogParseQueue = actionLogParseQueue;
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
                    _ParseQueue.Queue(new GameReplayParseQueueEntry() { GameID = proc.GameID, Force = true });
                } else if (proc.ReplaySimulated == null) {
                    _HeadlessRunQueue.Queue(new HeadlessRunQueueEntry() { GameID = proc.GameID, Force = true });
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
