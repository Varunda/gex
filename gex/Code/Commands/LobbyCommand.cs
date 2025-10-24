using gex.Code.ExtensionMethods;
using gex.Commands;
using gex.Common.Models.Lobby;
using gex.Services.Lobby;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using gex.Common.Models;
using gex.Common.Services.Lobby;

namespace gex.Code.Commands {

    [Command]
    public class LobbyCommand {

        private readonly ILogger<LobbyCommand> _Logger;
        private readonly LobbyManager _LobbyManager;
        private readonly ILobbyClient _LobbyClient;

        public LobbyCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<LobbyCommand>>();
            _LobbyManager = services.GetRequiredService<LobbyManager>();
            _LobbyClient = services.GetRequiredService<ILobbyClient>();
        }

        public void Summary() {
            List<LobbyBattle> battles = _LobbyManager.GetBattles();
            List<LobbyUser> users = _LobbyManager.GetUsers();
            Dictionary<string, LobbyUser> userDict = users.ToDictionaryDistinct(iter => iter.Username);

            int battlesOpened = battles.Count;
            int battlesActive = battles.Where(iter => {
                return userDict.GetValueOrDefault(iter.FounderUsername)?.InGame == true;
            }).Count();
            int battlesIdle = battles.Where(iter => {
                return userDict.GetValueOrDefault(iter.FounderUsername)?.InGame == false;
            }).Count();

            _Logger.LogInformation($"battles open: {battlesOpened}, active={battlesActive}/idle={battlesIdle}. users online: {users.Count}");
        }

        public void PrintBattles() {
            List<LobbyBattle> battles = _LobbyManager.GetBattles();
            List<LobbyUser> users = _LobbyManager.GetUsers();
            Dictionary<string, LobbyUser> userDict = users.ToDictionaryDistinct(iter => iter.Username);

            string s = $"Lobby battles - {battles.Count} opened:\n";
            foreach (LobbyBattle battle in battles) {
                string active = (userDict.GetValueOrDefault(battle.FounderUsername)?.InGame ?? false) ? "ACTIVE" : "IDLE";
                s += $"{battle.BattleID} - {battle.Map} - \"{battle.Title}\" ({active}) (founder={battle.FounderUsername})\n";
            }

            _Logger.LogInformation(s);
        }

        public void PrintUsers() {
            List<LobbyUser> users = _LobbyManager.GetUsers().OrderBy(iter => iter.Username).ToList();

            string s = $"Lobby users - {users.Count} online:\n";
            foreach (LobbyUser user in users) {
                s += $"{user.Username} - {user.UserID} [away={user.Away}] [in game={user.InGame}] [rank={user.Rank}] [is bot={user.IsBot}]\n";
            }

            _Logger.LogInformation(s);
        }

        public async Task Disconnect() {
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            _Logger.LogInformation($"disconnecting lobby");
            Result<bool, string> res = await _LobbyClient.Disconnect(cts.Token);
            if (res.IsOk == false) {
                _Logger.LogWarning($"error disconnecting [error={res.Error}]");
            } else {
                _LobbyManager.Clear();
                _Logger.LogInformation($"disconnect: {res.IsOk}");
            }
        }

        public void Battle(int battleID) {
            LobbyBattle? battle = _LobbyManager.GetBattle(battleID);
            if (battle == null) {
                _Logger.LogWarning($"failed to find battle [battleID={battleID}]");
                return;
            }

            LobbyUser? founder = _LobbyManager.GetUser(battle.FounderUsername);

            _Logger.LogInformation($"found battle [battleID={battleID}] [running={founder?.InGame ?? false}] [map={battle.Map}] [title={battle.Title}] "
                + $"[teamSize={battle.TeamSize}] [teamCount={battle.TeamCount}]");
            _Logger.LogInformation(JsonSerializer.Serialize(battle));
        }

        public void User(string username) {
            LobbyUser? user = _LobbyManager.GetUser(username);
            if (user == null) {
                _Logger.LogWarning($"failed to find user [username={username}]");
                return;
            }

            _Logger.LogInformation($"found user [username={user.Username}] [userID={user.UserID}] [version={user.Version}] "
                + $"[accessStatus={user.AccessStatus}] [away={user.Away}] [in game={user.InGame}] [is bot={user.IsBot}] [rank={user.Rank}]");
        }

        public async Task Whois(string username) {
            _Logger.LogInformation($"running $whois [username={username}]");
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            Result<LobbyWhoisResponse, string> res = await _LobbyClient.Whois(username, cts.Token);
            if (res.IsOk == true) {
                _Logger.LogDebug($"{JsonSerializer.Serialize(res.Value)}");
            }
        }

        public async Task GetDisconnected() {
            _Logger.LogInformation($"intentionally tripping flood protection");

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(120));

            List<LobbyBattle> battles = _LobbyManager.GetBattles();

            foreach (LobbyBattle battle in battles) {
                Result<LobbyBattleStatus, string> res = await _LobbyClient.BattleStatus(battle.BattleID, cts.Token);
                if (res.IsOk == true) {
                    _Logger.LogDebug($"{JsonSerializer.Serialize(res.Value)}");
                } else {
                    _Logger.LogWarning($"error getting battle status [founder={battle.FounderUsername}] [error={res.Error}]");
                }
            }

            _Logger.LogInformation($"done?");
        }

        public async Task BattleStatus(int battleID) {
            _Logger.LogInformation($"getting battle status for battle [battleID={battleID}]");

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            Result<LobbyBattleStatus, string> res = await _LobbyClient.BattleStatus(battleID, cts.Token);
            if (res.IsOk == true) {
                _Logger.LogDebug($"{JsonSerializer.Serialize(res.Value)}");
            } else {
                _Logger.LogWarning($"failed to get battle status [error={res.Error}]");
            }
        }

    }
}
