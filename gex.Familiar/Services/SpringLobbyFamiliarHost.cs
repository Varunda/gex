using gex.Common.Models;
using gex.Common.Models.Familiar;
using gex.Common.Models.Lobby;
using gex.Common.Services.Lobby;
using gex.Familiar.Models.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace gex.Familiar.Services {

    public class SpringLobbyFamiliarHost : IHostedService, IDisposable {

        private readonly ILogger<SpringLobbyFamiliarHost> _Logger;
        private readonly IOptions<SpringFamiliarOptions> _Options;
        private readonly ILobbyClient _LobbyClient;

        private CancellationTokenSource _Cancel = new();
        private Task? _PingTask;

        public SpringLobbyFamiliarHost(ILogger<SpringLobbyFamiliarHost> logger,
            IOptions<SpringFamiliarOptions> options, ILobbyClient lobbyClient) {

            _Logger = logger;
            _Options = options;
            _LobbyClient = lobbyClient;
        }

        public void Dispose() {
            _LobbyClient.OnMessageReceived -= _HandleMessage;
        }

        public Task StartAsync(CancellationToken cancel) {
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
            if (_Options.Value.Coordinator == "") {
                throw new Exception($"missing Spring:Coordinator. is this set in secrets.json?");
            }

            _LobbyClient.OnMessageReceived += _HandleMessage;

            Task.Run(async () => {
                await Loop(cancel);
            }, cancel);

            _PingTask = Task.Run(() => {
                _Logger.LogDebug($"started heartbeat task");
                try {
                    HeartbeatTask(_Cancel.Token);
                } catch (Exception) when (_Cancel.Token.IsCancellationRequested == true) {
                    _Logger.LogInformation($"closing heartbeat write thread safely due to cancellation token");
                } catch (Exception ex) when (_Cancel.Token.IsCancellationRequested == false) {
                    _Logger.LogError(ex, "error in heartbeat write thread");
                }
            }, _Cancel.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancel) {
            _Cancel.Cancel();
            if (_PingTask != null) {
                await _PingTask.WaitAsync(cancel);
            }

            Result<bool, string> disconnect = await _LobbyClient.Exit(cancel);

            if (disconnect.IsOk == false) {
                _Logger.LogWarning($"failed to disconnect from lobby [error={disconnect.Error}]");
            } else {
                _Logger.LogInformation($"disconnected safely from Spring");
            }
        }

        private async Task Loop(CancellationToken cancel) {
            _Logger.LogInformation($"familiar Spring lobby host started");

            int repeatedFailures = 0;

            while (cancel.IsCancellationRequested == false) {
                int delaySeconds = (3 * Math.Min(10, repeatedFailures)) + Random.Shared.Next(0, 3);

                try {
                    if (cancel.IsCancellationRequested == true) { break; }

                    if (_LobbyClient.IsConnected() == false) {
                        _Logger.LogInformation($"connecting to spring lobby");

                        if (cancel.IsCancellationRequested == true) { break; }

                        Result<bool, string> connect = await _LobbyClient.Connect(_Options.Value.Host, _Options.Value.Port, cancel);
                        if (connect.IsOk == false) {
                            _Logger.LogWarning($"failed to connect to lobby [error={connect.Error}] [delay={delaySeconds}]");
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
                            _Logger.LogWarning($"failed to login to lobby [error={login.Error}] [delay={delaySeconds}]");
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
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel);
                }
            }
        }

        private void _HandleMessage(object? eventSender, LobbyMessage message) {
            if (message.Command == "SAIDPRIVATE") {
                string? sender = message.GetWord();

                if (sender != _Options.Value.Coordinator && sender != "[Blahaj]varunda") {
                    return;
                }

                string? msg = message.GetSentence();
                _Logger.LogInformation($"DM gotten [sender={sender}] [msg='{msg}']");

            } else if (message.Command == "ADDUSER") {
            } else if (message.Command == "LEFTBATTLE") {
            } else if (message.Command == "UPDATEBATTLEINFO") {
            } else if (message.Command == "JOINEDBATTLE") {
            } else if (message.Command == "REMOVEUSER") {
            } else if (message.Command == "s.battle.update_lobby_title") {
            } else if (message.Command == "BATTLEOPENED") {
            } else if (message.Command == "BATTLECLOSED") {
            } else if (message.Command == "s.user.new_incoming_friend_request") {
            } else if (message.Command == "CLIENTSTATUS") {
                string? username = message.GetWord();
                if (username == _Options.Value.Username) {
                    _Logger.LogDebug($"{message.Command} {message.Arguments}");
                }
            } else if (message.Command == "MOTD") {
            } else if (message.Command == "TASSERVER") {
            } else if (message.Command == "SERVERMSG") {
            } else if (message.Command == "ACCEPTED") {
                _Logger.LogInformation($"login accepted");
            } else if (message.Command == "JOINBATTLE") {
                _Logger.LogInformation($"joined battle");
            } else if (message.Command == "SETSCRIPTTAGS") {
            } else if (message.Command == "CLIENTBATTLESTATUS") {
                _Logger.LogInformation($"{message.Command} {message.Arguments}");
            } else if (message.Command == "ADDSTARTRECT") {
            } else if (message.Command == "REQUESTBATTLESTATUS") {
            } else if (message.Command == "s.battle.teams") { 
            } else if (message.Command == "SAIDBATTLEEX") {
                _Logger.LogInformation($"message in lobby [args={message.Arguments}]");

                string? username = message.GetWord();
                string? msg = message.GetSentence();

                if ((msg?.Contains("Unable to start game,") ?? false) && (msg?.Contains("is unsynced") ?? false) && (msg?.Contains(_Options.Value.Username) ?? false)) {
                    _Logger.LogInformation($"familiar is unsynced, resyncing!");
                    _LobbyClient.Write("MYBATTLESTATUS", $"{(1 << 22) | (1 << 1)} 0", CancellationToken.None);
                }
            } else {
                _Logger.LogWarning($"unhandled message type [command={message.Command}] [args={message.Arguments}]");
            }
        }

        private async void HeartbeatTask(CancellationToken cancel) {
            _Logger.LogInformation($"ping write started");
            while (cancel.IsCancellationRequested == false) {
                int delaySeconds = Random.Shared.Next(1, 3);

                try {
                    if (_LobbyClient.IsLoggedIn() == false) {
                        _Logger.LogDebug($"client not logged in, not sending heartbeat [delay={delaySeconds}]");
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel);
                        continue;
                    }

                    /*
                    Result<LobbyMessage, string> msg = await _SendRequest(new FamiliarMessageHeartbeatRequest(), cancel);
                    if (msg.IsOk == false) {
                        _Logger.LogWarning($"error sending heartbeat to coordinator: {msg.Error}");
                    }
                    */

                    await Task.Delay(TimeSpan.FromSeconds(30), cancel);
                } catch (Exception) when (cancel.IsCancellationRequested == true) {
                    return;
                } catch (Exception ex) when (cancel.IsCancellationRequested == false) {
                    _Logger.LogError(ex, $"error in ping write thread");
                }
            }
        }

    }
}
