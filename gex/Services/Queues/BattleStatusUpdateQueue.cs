using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class BattleStatusUpdateQueue : BaseQueue<BattleStatusUpdateQueueEntry> {

        public BattleStatusUpdateQueue(ILoggerFactory factory, QueueMetric metrics)
            : base(factory, metrics) { }

    }
}
