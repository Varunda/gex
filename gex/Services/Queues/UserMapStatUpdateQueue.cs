using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class UserMapStatUpdateQueue : BaseQueue<UserMapStatUpdateQueueEntry> {

        public UserMapStatUpdateQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
