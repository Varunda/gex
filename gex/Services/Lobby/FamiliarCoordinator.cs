using gex.Code.Hubs;
using gex.Code.Hubs.Implementations;
using gex.Common.Models.Familiar;
using gex.Common.Models.Lobby;
using gex.Models.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp;

namespace gex.Services.Lobby {

    public class FamiliarCoordinator {

        private readonly ILogger<FamiliarCoordinator> _Logger;
        private readonly IHubContext<FamiliarHub, IFamiliarHub> _Hub;
        private readonly IOptions<SpringLobbyOptions> _Options;

        private static readonly Dictionary<string, FamiliarStatus> _Statuses = new();

        public FamiliarCoordinator(ILogger<FamiliarCoordinator> logger,
            IOptions<SpringLobbyOptions> options, IHubContext<FamiliarHub, IFamiliarHub> hub) {

            _Logger = logger;
            _Options = options;
            _Hub = hub;
        }

        public FamiliarStatus? GetAvailableFamiliar() {
            lock (_Statuses) {
                foreach (FamiliarStatus status in _Statuses.Values) {
                    if (status.InBattle == false && status.HasLua == true) {
                        return status;
                    }
                }
            }

            return null;
        }

        public void UpdateStatus(FamiliarStatus status) {
            lock (_Statuses) {
                _Statuses[status.Name] = status;
            }
            _Logger.LogDebug($"status update from familiar [familiar={status.Name}] [in battle={status.InBattle}] [battle id={status.BattleID}] [secret={status.Secret}]");
        }

        public List<FamiliarStatus> GetStatuses() {
            lock (_Statuses) {
                return _Statuses.Values.ToList();
            }
        }

        public async Task SendFamiliarToBattle(string name, FamiliarJoinBattleMessage msg) {
            await _Hub.Clients.User(name).JoinGame(msg);
        }

        public async Task LaunchFamiliarGame(string name, FamiliarLaunchGameMessage msg) {
            await _Hub.Clients.User(name).LaunchGame(msg);
        }

        public async Task LeaveLobby(string name, FamiliarLeaveLobbyMessage msg) {
            await _Hub.Clients.User(name).LeaveLobby(msg);
        }

    }
}
