using gex.Common.Models;
using gex.Common.Models.Lobby;
using gex.Common.Services.Lobby;
using gex.Models.Options;
using gex.Services.Queues;
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
    public class SpringLobbyClientHost : IHostedService, IDisposable {

        private readonly ILogger<SpringLobbyClientHost> _Logger;
        private readonly IOptions<SpringLobbyOptions> _Options;
        private readonly ILobbyClient _LobbyClient;
        private readonly BaseQueue<LobbyMessage> _LobbyMessageQueue;

        public SpringLobbyClientHost(ILogger<SpringLobbyClientHost> logger,
            IOptions<SpringLobbyOptions> options, ILobbyClient lobbyClient,
            BaseQueue<LobbyMessage> lobbyMessageQueue) {

            _Logger = logger;
            _Options = options;
            _LobbyClient = lobbyClient;
            _LobbyMessageQueue = lobbyMessageQueue;
        }

        public void Dispose() {
            _LobbyClient.OnMessageReceived -= _HandleMessage;
        }

        public Task StartAsync(CancellationToken cancel) {
            if (_Options.Value.Enabled == false) {
                _Logger.LogInformation($"spring is disabled, not starting client");
                return Task.CompletedTask;
            }

            if (_Options.Value.Host == "") {
                throw new Exception($"missing Spring:Host. is this set in secrets.json?");
            }
            if (_Options.Value.Port == 0) {
                throw new Exception($"missing Spring:Port. is this set in secrets.json?");
            }
            if (_Options.Value.Username == "") {
                throw new Exception($"missing Spring:Username. is this set in secrets.json?");
            }
            if (_Options.Value.Password == "") {
                throw new Exception($"missing Spring:Password. is this set in secrets.json?");
            }

            _LobbyClient.OnMessageReceived += _HandleMessage;

            Task.Run(async () => {
                await Loop(cancel);
            }, cancel);

            return Task.CompletedTask;
        }

        private void _HandleMessage(object? sender, LobbyMessage message) {
            _LobbyMessageQueue.Queue(message);
        }

        private async Task Loop(CancellationToken cancel) {

            int repeatedFailures = 0;

            while (cancel.IsCancellationRequested == false) {
                try {
                    if (cancel.IsCancellationRequested == true) { break; }

                    int delaySeconds = (3 * Math.Min(10, repeatedFailures)) + Random.Shared.Next(0, 3);

                    if (_LobbyClient.IsConnected() == false) {
                        _Logger.LogInformation($"connecting to spring lobby");

                        if (cancel.IsCancellationRequested == true) { break; }

                        Result<bool, string> connect = await _LobbyClient.Connect(_Options.Value.Host, _Options.Value.Port, cancel);
                        if (connect.IsOk == false) {
                            _Logger.LogWarning($"failed to connect to lobby [error={connect.Error}]");
                            ++repeatedFailures;
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel);
                            continue;
                        }
                        if (cancel.IsCancellationRequested == true) { break; }

                        _Logger.LogDebug($"connected to spring lobby");
                    } else if (_LobbyClient.IsLoggedIn() == false && _LobbyClient.IsLoggingIn() == false) {
                        _Logger.LogInformation($"client is not logged in, logging in");

                        Result<LobbyMessage, string> login = await _LobbyClient.Login(_Options.Value.Username, _Options.Value.Password, cancel);
                        if (login.IsOk == false) {
                            _Logger.LogWarning($"failed to login to lobby [error={login.Error}]");
                            ++repeatedFailures;
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel);
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
                                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel);
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
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel);
                            continue;
                        }
                        if (cancel.IsCancellationRequested == true) { break; }
                    }

                    await Task.Delay(10, cancel); // delay a bit so thread is not running at 100% speed lol

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
