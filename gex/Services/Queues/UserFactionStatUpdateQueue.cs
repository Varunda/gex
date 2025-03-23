using gex.Models.Queues;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class UserFactionStatUpdateQueue : BaseQueue<UserFactionStatUpdateQueueEntry> {

        public UserFactionStatUpdateQueue(ILoggerFactory factory) : base(factory) { }

    }
}
