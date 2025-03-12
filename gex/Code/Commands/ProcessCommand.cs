using gex.Commands;
using gex.Models.Queues;
using gex.Services.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace gex.Code.Commands {

    [Command]
    public class ProcessCommand {

        private readonly ILogger<ProcessCommand> _Logger;
        private readonly BaseQueue<GameReplayDownloadQueueEntry> _Queue;

        public ProcessCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<ProcessCommand>>();
            _Queue = services.GetRequiredService<BaseQueue<GameReplayDownloadQueueEntry>>();
        }

        public void ForceRun(string gameID) {
            _Logger.LogInformation($"forcing a reprocess of game [gameID={gameID}]");
            _Queue.Queue(new GameReplayDownloadQueueEntry() {
                GameID = gameID,
                Force = true
            });
        }

    }
}
