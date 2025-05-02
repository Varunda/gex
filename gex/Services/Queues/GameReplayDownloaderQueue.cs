using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class GameReplayDownloaderQueue : BaseQueue<GameReplayDownloadQueueEntry> {

        public GameReplayDownloaderQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
