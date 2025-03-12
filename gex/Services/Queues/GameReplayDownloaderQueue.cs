using gex.Models.Queues;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class GameReplayDownloaderQueue : BaseQueue<GameReplayDownloadQueueEntry> {

        public GameReplayDownloaderQueue(ILoggerFactory factory) : base(factory) { }

    }
}
