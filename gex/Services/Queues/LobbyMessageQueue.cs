using gex.Common.Models.Lobby;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class LobbyMessageQueue : BaseQueue<LobbyMessage> {

        public LobbyMessageQueue(ILoggerFactory factory, QueueMetric metrics)
            : base(factory, metrics) { }

    }
}
