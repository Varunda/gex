using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class SubscriptionMessageQueue : BaseQueue<SubscriptionMessageQueueEntry> {

        public SubscriptionMessageQueue(ILoggerFactory factory, QueueMetric metrics)
            : base(factory, metrics) { }

    }
}
