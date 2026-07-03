using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class MatchProcessingWebhookQueue : BaseQueue<MatchProcessingWebhookQueueEntry> {

        public MatchProcessingWebhookQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
