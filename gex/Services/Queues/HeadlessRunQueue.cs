using gex.Models.Queues;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class HeadlessRunQueue : BaseQueue<HeadlessRunQueueEntry> {

        public HeadlessRunQueue(ILoggerFactory factory) : base(factory) { }

    }
}
