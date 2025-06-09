using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class MapStatUpdateQueue : BaseQueue<MapStatUpdateQueueEntry> {

        public MapStatUpdateQueue(ILoggerFactory factory, QueueMetric metrics)
            : base(factory, metrics) { }

    }
}
