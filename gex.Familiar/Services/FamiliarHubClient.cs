using gex.Common.Models;
using gex.Common.Models.Familiar;
using gex.Common.Models.Lobby;
using gex.Common.Models.Options;
using gex.Common.Services.Lobby;
using gex.Familiar.Models.Options;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Familiar.Services {

    public class FamiliarHubClient {

        private readonly ILogger<FamiliarHubClient> _Logger;
        private readonly IOptions<JwtFamiliarOptions> _Options;
        private readonly IOptions<FileStorageOptions> _StorageOptions;
        private readonly StatusHolder _Status;
        private readonly ILobbyClient _LobbyClient;
        private readonly LobbyManager _LobbyManager;
        private readonly GameInstance _GameInstance;

        private readonly HubConnection _Connection;

        public FamiliarHubClient(ILogger<FamiliarHubClient> logger,
            IOptions<JwtFamiliarOptions> options, StatusHolder status,
            ILobbyClient lobbyClient, LobbyManager lobbyManager,
            IOptions<FileStorageOptions> storageOptions, GameInstance gameInstance) {

            _Logger = logger;
            _Options = options;
            _Status = status;
            _LobbyClient = lobbyClient;
            _LobbyManager = lobbyManager;
            _StorageOptions = storageOptions;

            _Logger.LogDebug($"creating signalR connection [host={_Options.Value.Host}]");

            _Connection = new HubConnectionBuilder()
                .WithUrl(_Options.Value.Host, (HttpConnectionOptions options) => {
                    options.AccessTokenProvider = () => {
                        return Task.FromResult((string?)_Options.Value.Token);
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _Connection.On<string>("Hello", OnHello);
            _Connection.On<FamiliarSendWidgetMessage>("SendLua", OnSendLua);
            _Connection.On<FamiliarJoinBattleMessage>("JoinGame", OnJoinGame);
            _Connection.On<FamiliarLaunchGameMessage>("LaunchGame", OnLaunchGame);
            _Connection.On<FamiliarLeaveLobbyMessage>("LeaveLobby", OnLeaveLobby);

            _Connection.Closed += _OnClosed;
            _Connection.Reconnecting += _OnReconnecting;
            _Connection.Reconnected += _OnReconnected;
            _GameInstance = gameInstance;
        }

        public async Task Start(CancellationToken cancel) {
            if (_Connection.State == HubConnectionState.Disconnected) {
                await _Connection.StartAsync(cancel);
                _Logger.LogInformation($"connection made");
            } else {
                _Logger.LogWarning($"not connecting to hub, not disconnected [state={_Connection.State}]");
            }
        }

        private Task _OnReconnecting(Exception? ex) {
            _Logger.LogDebug($"reconnecting... [ex={ex?.Message}]");
            return Task.CompletedTask;
        }

        private Task _OnReconnected(string? connectionId) {
            _Logger.LogDebug($"reconnected");
            return Task.CompletedTask;
        }

        private Task _OnClosed(Exception? ex) {
            _Logger.LogWarning($"hub connection closed [ex={ex?.Message}]");
            return Task.CompletedTask;
        }

        public bool IsDisconnected() {
            return _Connection.State == HubConnectionState.Disconnected;
        }

        private async Task OnHello(string familiarName) {
            _Logger.LogDebug($"gex said hello [name={familiarName}]");

            await _UpdateStatus(status => status.Name = familiarName);
        }

        private async Task OnSendLua(FamiliarSendWidgetMessage msg) {
            _Logger.LogDebug($"got gex.lua and BYAR.lua from host");

            string gexWidgetPath = Path.Join(_StorageOptions.Value.TempWorkLocation, "gex.lua");
            await File.WriteAllTextAsync(gexWidgetPath, msg.Code);
            _Logger.LogDebug($"wrote gex.lua [path={gexWidgetPath}]");

            string byarPath = Path.Join(_StorageOptions.Value.TempWorkLocation, "BYAR.lua");
            await File.WriteAllTextAsync(byarPath, msg.Byar);
            _Logger.LogDebug($"wrote BYAR.lua [path={byarPath}]");

            await _UpdateStatus(status => status.HasLua = true);
        }

        private async Task OnJoinGame(FamiliarJoinBattleMessage msg) {
            _Logger.LogDebug($"joining battle [battleID={msg.BattleID}]");

            if (_LobbyClient.IsLoggedIn() == false) {
                _Logger.LogWarning($"familiar is not logged in, cannot join battle");
                return;
            }

            Result<bool, string> res = await _LobbyClient.Write("JOINBATTLE", $"{msg.BattleID} {(msg.Password ?? "empty")} {msg.Secret}", CancellationToken.None);

            if (res.IsOk == false) {
                _Logger.LogError($"failed to join battle [battleID={msg.BattleID}] [error={res.Error}]");
            } else {
                await _UpdateStatus(status => {
                    status.BattleID = msg.BattleID;
                    status.Secret = msg.Secret;
                });

                await Task.Delay(1000, CancellationToken.None);
                _Logger.LogInformation($"joined battle [battleID={msg.BattleID}]");

                // lowest bit indicates in game or not
                await _LobbyClient.Write("MYSTATUS", $"{0}", CancellationToken.None);
                // b1 is used for ready or not, 22 is for synced
                await _LobbyClient.Write("MYBATTLESTATUS", $"{(1 << 22) | (1 << 1)} 0", CancellationToken.None);
                // say hello
                await _LobbyClient.Write("SAYBATTLE", $"{msg.InvitedBy} invited Gex to watch this game in realtime", CancellationToken.None);
            }
        }

        private async Task OnLaunchGame(FamiliarLaunchGameMessage msg) {
            _Logger.LogInformation($"launching game [battleID={msg.BattleID}] [ip={msg.HostIP}] [port={msg.Port}]");

            await _UpdateStatus(status => {
                status.BattleID = msg.BattleID;
                status.InBattle = true;
                status.Secret = msg.Secret;
            });

            await _GameInstance.Run(msg, CancellationToken.None);
        }

        private async Task OnLeaveLobby(FamiliarLeaveLobbyMessage msg) {
            _Logger.LogInformation($"leaving lobby");

            _GameInstance.Kill();

            await _LobbyClient.Write("LEAVEBATTLE", "", CancellationToken.None);

            await _UpdateStatus((FamiliarStatus status) => {
                status.BattleID = null;
                status.InBattle = false;
                status.Secret = 0;
            });
        }

        private async Task _UpdateStatus(Action<FamiliarStatus> changes) {
            FamiliarStatus status = _Status.Get();
            changes(status);
            _Status.Update(status);
            await _Connection.InvokeAsync("StatusUpdate", status);
        }

    }
}
