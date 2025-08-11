using gex.Models;
using gex.Models.Lobby;
using gex.Services.Lobby;
using gex.Services.Lobby.Implementations;
using gex.Services.Parser;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class StartupTestService : BackgroundService {

        private readonly ILogger<StartupTestService> _Logger;
        private readonly ILobbyClient _LobbyClient;

        public StartupTestService(ILogger<StartupTestService> logger, 
            ILobbyClient lobbyClient) {

            _Logger = logger;
            _LobbyClient = lobbyClient;
        }

        protected override Task ExecuteAsync(CancellationToken cancel) {
            return Task.Run(async () => {
                await Task.Delay(TimeSpan.FromMilliseconds(1));
            }, cancel);
        }

    }
}
