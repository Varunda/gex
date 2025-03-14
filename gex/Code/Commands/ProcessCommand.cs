﻿using gex.Commands;
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
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogQueue;

        public ProcessCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<ProcessCommand>>();
            _Queue = services.GetRequiredService<BaseQueue<GameReplayDownloadQueueEntry>>();
            _ActionLogQueue = services.GetRequiredService<BaseQueue<ActionLogParseQueueEntry>>();
        }

        public void ForceRun(string gameID) {
            _Logger.LogInformation($"forcing a reprocess of game [gameID={gameID}]");
            _Queue.Queue(new GameReplayDownloadQueueEntry() {
                GameID = gameID,
                Force = true,
                ForceForward = true
            });
        }

        /// <summary>
        ///     command to re-parse the action log of a game
        /// </summary>
        /// <param name="gameID"></param>
        public void ParseActions(string gameID) {
            _Logger.LogInformation($"forcing a re-process of action log [gameID={gameID}]");
            _ActionLogQueue.Queue(new ActionLogParseQueueEntry() {
                GameID = gameID,
                Force = true
            });
        }

    }
}
