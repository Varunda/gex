using gex.Models;
using gex.Models.Lobby;
using gex.Models.Options;
using gex.Services.Lobby;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.BackgroundTasks {

    /// <summary>
    ///     background service that hosts the spring lobby client (if enabled), performing the login and such
    /// </summary>
    public class SpringLobbyClientHost : IHostedService {

        private readonly ILogger<SpringLobbyClientHost> _Logger;
        private readonly IOptions<SpringLobbyOptions> _Options;
        private readonly ILobbyClient _LobbyClient;

        public SpringLobbyClientHost(ILogger<SpringLobbyClientHost> logger,
            IOptions<SpringLobbyOptions> options, ILobbyClient lobbyClient) {

            _Logger = logger;
            _Options = options;
            _LobbyClient = lobbyClient;
        }


        public Task StartAsync(CancellationToken cancel) {
            if (_Options.Value.Enabled == false) {
                _Logger.LogInformation($"spring is disabled, not starting client");
                return Task.CompletedTask;
            }

            return Task.Run(async () => {
                _Logger.LogInformation($"connecting to spring lobby");

                Result<bool, string> connect = await _LobbyClient.Connect(cancel);
                if (connect.IsOk == false) {
                    _Logger.LogWarning($"failed to connect to lobby: {connect.Error}");
                    return;
                }

                Result<LobbyMessage, string> login = await _LobbyClient.Login(cancel);
                if (login.IsOk == false) {
                    _Logger.LogWarning($"failed to login to lobby: {login.Error}");
                    return;
                }

                if (login.Value.Command == "DENIED") {
                    _Logger.LogWarning($"login to lobby failed [reason={login.Value.Arguments}]");
                } else {
                    _Logger.LogInformation($"login response: {login.Value.Command}");
                }
            }, cancel);
        }

        public async Task StopAsync(CancellationToken cancel) {
            Result<bool, string> disconnect = await _LobbyClient.Disconnect(cancel);

            if (disconnect.IsOk == false) {
                _Logger.LogWarning($"failed to disconnect from lobby [error={disconnect.Error}]");
            } else {
                _Logger.LogInformation($"disconnected safely from Spring");
            }
        }

    }
}
