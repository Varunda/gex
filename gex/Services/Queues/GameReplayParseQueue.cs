using gex.Models.Queues;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class GameReplayParseQueue : BaseQueue<GameReplayParseQueueEntry> {

        public GameReplayParseQueue(ILoggerFactory factory) : base(factory) { }

    }
}
