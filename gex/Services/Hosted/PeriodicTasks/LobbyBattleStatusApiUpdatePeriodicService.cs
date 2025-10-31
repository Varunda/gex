using gex.Common.Models;
using gex.Services.BarApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using gex.Common.Models.Lobby;
using gex.Common.Services.Lobby;

namespace gex.Services.Hosted.PeriodicTasks {

    /// <summary>
    ///     updates the lobby battle status from the BAR API
    /// </summary>
    public class LobbyBattleStatusApiUpdatePeriodicService : AppBackgroundPeriodicService {

        private const string SERVICE_NAME = "lobby_battle_status_api_update";

        private static readonly TimeSpan RUN_DELAY = TimeSpan.FromSeconds(60);

        private readonly BarBattleStatusApi _BattleStatusApi;
        private readonly LobbyManager _LobbyManager;
        private readonly ILobbyClient _LobbyClient;

        public LobbyBattleStatusApiUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarBattleStatusApi battleStatusApi,
            LobbyManager lobbyManager, ILobbyClient lobbyClient)
        : base(SERVICE_NAME, RUN_DELAY, loggerFactory, healthMon) {
            _BattleStatusApi = battleStatusApi;
            _LobbyManager = lobbyManager;
            _LobbyClient = lobbyClient;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"updating battle status from BAR API");

            if (_LobbyClient.IsLoggedIn() == false) {
                _Logger.LogWarning($"not updating battle status, client is not logged in");
                return "client not logged in, not updating";
            }

            Result<List<LobbyBattleStatus>, string> res = await _BattleStatusApi.GetAll(cancel);
            if (res.IsOk == false) {
                _Logger.LogWarning($"failed to update battle status [error={res.Error}]");
                return $"error calling api: {res.Error}";
            }

            List<LobbyBattleStatus> statuses = res.Value;

            foreach (LobbyBattleStatus status in statuses) {
                LobbyBattle? battle = _LobbyManager.GetBattle(status.BattleID);
                if (battle == null) {
                    _Logger.LogWarning($"missing battle to update with the new battle status [battleID={status.BattleID}]");
                    continue;
                }

                battle.BattleStatus = status;
                _LobbyManager.UpdateBattle(status.BattleID, battle);
            }

            _Logger.LogInformation($"done updating lobby battle status from API [count={statuses.Count}]");
            return $"updated {statuses.Count} battle lobby statuses";
        }

    }
}
