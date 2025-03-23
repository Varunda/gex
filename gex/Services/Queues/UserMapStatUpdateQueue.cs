using gex.Models.Queues;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class UserMapStatUpdateQueue : BaseQueue<UserMapStatUpdateQueueEntry> {

        public UserMapStatUpdateQueue(ILoggerFactory factory) : base(factory) { }

    }
}
