using gex.Models.Api;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class HeadlessRunStatusUpdateQueue : BaseQueue<HeadlessRunStatus> {

        public HeadlessRunStatusUpdateQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
