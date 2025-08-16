using gex.Models;
using gex.Models.Lobby;
using gex.Models.Options;
using gex.Services.Lobby;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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

            Task.Run(async () => {
                await Loop(cancel);
            }, cancel);

            return Task.CompletedTask;
        }

        private async Task Loop(CancellationToken cancel) {

            int repeatedFailures = 0;

            while (cancel.IsCancellationRequested == false) {
                try {
                    if (_LobbyClient.IsConnected() == false) {
                        _Logger.LogInformation($"connecting to spring lobby");

                        int delaySeconds = 3 * Math.Min(10, repeatedFailures);

                        if (cancel.IsCancellationRequested == true) { break; }

                        Result<bool, string> connect = await _LobbyClient.Connect(cancel);
                        if (connect.IsOk == false) {
                            _Logger.LogWarning($"failed to connect to lobby [error={connect.Error}]");
                            ++repeatedFailures;
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds + Random.Shared.Next(0, 3)), cancel);
                            continue;
                        }
                        if (cancel.IsCancellationRequested == true) { break; }

                        _Logger.LogDebug($"connected to spring lobby");

                        Result<LobbyMessage, string> login = await _LobbyClient.Login(cancel);
                        if (login.IsOk == false) {
                            _Logger.LogWarning($"failed to login to lobby [error={login.Error}]");
                            ++repeatedFailures;
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds + Random.Shared.Next(0, 3)), cancel);
                            continue;
                        }
                        if (cancel.IsCancellationRequested == true) { break; }

                        if (login.Value.Command == "DENIED") {
                            if (login.Value.Arguments.StartsWith("Flood protection - Please wait")) {
                                _Logger.LogWarning($"hit flood protection for login, delaying 22 seconds");
                                await Task.Delay(TimeSpan.FromSeconds(22), cancel);
                            } else {
                                _Logger.LogError($"login to lobby failed [reason={login.Value.Arguments}]");
                                ++repeatedFailures;
                                await Task.Delay(TimeSpan.FromSeconds(delaySeconds + Random.Shared.Next(0, 3)), cancel);
                            }

                            await _LobbyClient.Disconnect(cancel);

                            continue;
                        }
                        if (cancel.IsCancellationRequested == true) { break; }

                        repeatedFailures = 0;
                        _Logger.LogInformation($"login response: {login.Value.Command}");
                    } else {
                        TimeSpan lastMessageDiff = DateTime.UtcNow - _LobbyClient.LastMessage();
                        // a PING is sent every 30 seconds, so do a bit longer to let the PONG response reset this timer as well
                        if (lastMessageDiff >= TimeSpan.FromSeconds(40)) {
                            _Logger.LogWarning($"last message is over 40 seconds ago, reconnecting [diff={lastMessageDiff}]");

                            await _LobbyClient.Disconnect(cancel);
                            ++repeatedFailures;
                            await Task.Delay(TimeSpan.FromSeconds((3 * repeatedFailures) + Random.Shared.Next(0, 3)), cancel);
                            continue;
                        }
                        if (cancel.IsCancellationRequested == true) { break; }
                    }
                } catch (Exception) when (cancel.IsCancellationRequested == true) {
                    _Logger.LogInformation($"closing lobby client host");
                } catch (Exception ex) when (cancel.IsCancellationRequested == false) {
                    _Logger.LogError(ex, $"error in lobby client host");
                }
            }
        }

        public async Task StopAsync(CancellationToken cancel) {
            Result<bool, string> disconnect = await _LobbyClient.Exit(cancel);

            if (disconnect.IsOk == false) {
                _Logger.LogWarning($"failed to disconnect from lobby [error={disconnect.Error}]");
            } else {
                _Logger.LogInformation($"disconnected safely from Spring");
            }
        }

    }
}
