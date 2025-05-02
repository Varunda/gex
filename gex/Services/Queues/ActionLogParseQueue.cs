using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class ActionLogParseQueue : BaseQueue<ActionLogParseQueueEntry> {

        public ActionLogParseQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
