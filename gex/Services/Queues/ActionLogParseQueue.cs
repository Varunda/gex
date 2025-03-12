using gex.Models.Queues;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class ActionLogParseQueue : BaseQueue<ActionLogParseQueueEntry> {

        public ActionLogParseQueue(ILoggerFactory factory) : base(factory) { }

    }
}
