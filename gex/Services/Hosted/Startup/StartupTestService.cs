using gex.Models;
using gex.Models.Lobby;
using gex.Services.Lobby;
using gex.Services.Lobby.Implementations;
using gex.Services.Parser;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class StartupTestService : BackgroundService {

        private readonly ILogger<StartupTestService> _Logger;
        private readonly ILobbyClient _LobbyClient;
        private readonly LobbyManager _LobbyManager;

        public StartupTestService(ILogger<StartupTestService> logger,
            ILobbyClient lobbyClient, LobbyManager lobbyManager) {

            _Logger = logger;
            _LobbyClient = lobbyClient;
            _LobbyManager = lobbyManager;
        }

        protected override Task ExecuteAsync(CancellationToken cancel) {
            Task.Run(async () => {
                _Logger.LogInformation($"waiting for client to be logged in");
                while (_LobbyClient.IsLoggedIn() == false) {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }

                _Logger.LogInformation($"client is now connected");

                await Task.Delay(TimeSpan.FromMilliseconds(1000));
            }, cancel);

            return Task.CompletedTask;
        }

    }
}
