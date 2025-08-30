using gex.Services.Db.UserStats;
using gex.Services.Lobby;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class LobbyAlertSendingPeriodicService : AppBackgroundPeriodicService {

        private const string SERVICE_NAME = "lobby_alert_sending";

        private readonly LobbyManager _LobbyManager;
        private readonly BarUserRepository _UserRepository;

        public LobbyAlertSendingPeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, LobbyManager lobbyManager,
            BarUserRepository userRepository)
        : base(SERVICE_NAME, TimeSpan.FromMinutes(3), loggerFactory, healthMon) {

            _LobbyManager = lobbyManager;
            _UserRepository = userRepository;
        }

        protected override Task<string?> PerformTask(CancellationToken cancel) {
            throw new System.NotImplementedException();
        }

    }
}
