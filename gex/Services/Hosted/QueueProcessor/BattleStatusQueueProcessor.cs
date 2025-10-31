using gex.Common.Models;
using gex.Common.Models.Lobby;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Common.Services.Metrics;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using gex.Common.Services.Lobby;

namespace gex.Services.Hosted.QueueProcessor {

    public class BattleStatusQueueProcessor : BaseQueueProcessor<BattleStatusUpdateQueueEntry> {

        private readonly LobbyManager _LobbyManager;
        private readonly ILobbyClient _LobbyClient;
        private readonly LobbyClientMetric _LobbyMetric;

        private DateTime _LastSent = DateTime.MinValue;

        private static Dictionary<int, DateTime> _LastUpdate = [];

        public BattleStatusQueueProcessor(ILoggerFactory factory,
            BaseQueue<BattleStatusUpdateQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            LobbyManager lobbyManager, ILobbyClient lobbyClient,
            LobbyClientMetric lobbyMetric)
        : base("battle_status_update_queue", factory, queue, serviceHealthMonitor) {

            _LobbyManager = lobbyManager;
            _LobbyClient = lobbyClient;
            _LobbyMetric = lobbyMetric;
        }

        protected override async Task<bool> _ProcessQueueEntry(BattleStatusUpdateQueueEntry entry, CancellationToken cancel) {
            DateTime lastUpdated = _LastUpdate.GetValueOrDefault(entry.BattleID);
            TimeSpan updateDiff = DateTime.UtcNow - lastUpdated;
            if (updateDiff < TimeSpan.FromSeconds(5)) {
                //_Logger.LogDebug($"not performing update for battle, too recently updated [battleID={entry.BattleID}] [updateDiff={updateDiff}]");
                return false;
            }

            TimeSpan diff = DateTime.UtcNow - _LastSent;
            if (diff < TimeSpan.FromMilliseconds(100)) {
                //_Logger.LogDebug($"hit self imposted rate limit for battle status update [battleID={entry.BattleID}] [diff={diff}]");
                if (diff > TimeSpan.Zero) {
                    await Task.Delay(TimeSpan.FromMilliseconds(100) - diff, cancel);
                }
            }

            if (_LobbyClient.IsLoggedIn() == false) {
                _Logger.LogInformation($"waiting a bit for lobby client to be logged in (cannot get battle status) [battleID={entry.BattleID}]");
                await Task.Delay(TimeSpan.FromSeconds(5), cancel);
            }

            if (_LobbyClient.IsLoggedIn() == false) {
                _Logger.LogWarning($"cannot update battle status, lobby client is not logged in");
                return false;
            }

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(4));

            // calling this updates the battle status field
            Result<LobbyBattleStatus, string> res = await _LobbyClient.BattleStatus(entry.BattleID, cts.Token);
            if (res.IsOk == false) {
                _Logger.LogWarning($"failed to update battle status [battleID={entry.BattleID}] [error={res.Error}]");
            }

            _LastUpdate[entry.BattleID] = DateTime.UtcNow;
            _LastSent = DateTime.UtcNow;
            _Logger.LogTrace($"updated battle status [battleID={entry.BattleID}] [reason={entry.Reason}]");
            _LobbyMetric.RecordBattleStatus(entry.Reason);

            return true;
        }

    }
}
