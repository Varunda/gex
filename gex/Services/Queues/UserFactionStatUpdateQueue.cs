using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class UserFactionStatUpdateQueue : BaseQueue<UserFactionStatUpdateQueueEntry> {

        public UserFactionStatUpdateQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
