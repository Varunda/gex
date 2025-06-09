using gex.Models.Api;
using gex.Services.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gex.Code.Hubs {

    public class HeadlessReplayHub : Hub<IHeadlessReplayHub> {

        private readonly ILogger<HeadlessReplayHub> _Logger;
        private readonly HeadlessRunStatusRepository _HeadlessRunStatusRepository;

        private static Dictionary<string, string> _PreviousSubscribedGame = [];

        public HeadlessReplayHub(ILogger<HeadlessReplayHub> logger,
            HeadlessRunStatusRepository headlessRunStatusRepository) {

            _Logger = logger;
            _HeadlessRunStatusRepository = headlessRunStatusRepository;
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            _PreviousSubscribedGame.Remove(Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToMatch(string gameID) {
            string connID = Context.ConnectionId;

            string? previousGameID = null;
            lock (_PreviousSubscribedGame) {
                if (_PreviousSubscribedGame.ContainsKey(connID)) {
                    previousGameID = _PreviousSubscribedGame.GetValueOrDefault(connID);
                }
                _PreviousSubscribedGame[connID] = gameID;
            }

            if (previousGameID != null) {
                await Groups.RemoveFromGroupAsync(connID, $"Gex.Headless.{previousGameID}");
            }

            await Groups.AddToGroupAsync(connID, $"Gex.Headless.{gameID}");

            HeadlessRunStatus? status = _HeadlessRunStatusRepository.Get(gameID);
            if (status != null) {
                await Clients.Caller.UpdateProgress(status);
            }

        }

    }
}
