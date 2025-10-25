using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class FixCountryCodeQueue : BaseQueue<FixCountryCodeQueueEntry> {

        public FixCountryCodeQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
