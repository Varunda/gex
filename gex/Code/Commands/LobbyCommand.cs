using gex.Code.ExtensionMethods;
using gex.Commands;
using gex.Models;
using gex.Models.Lobby;
using gex.Services.Lobby;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public void User(string username) {
            LobbyUser? user = _LobbyManager.GetUser(username);
            if (user == null) {
                _Logger.LogWarning($"failed to find user [username={username}]");
                return;
            }

            _Logger.LogInformation($"found user [username={user.Username}] [userID={user.UserID}] [version={user.Version}] "
                + $"[accessStatus={user.AccessStatus}] [away={user.Away}] [in game={user.InGame}] [is bot={user.IsBot}] [rank={user.Rank}]");
        }

    }
}
