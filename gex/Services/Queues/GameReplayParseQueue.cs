using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class GameReplayParseQueue : BaseQueue<GameReplayParseQueueEntry> {

        public GameReplayParseQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
