using gex.Code.ExtensionMethods;
using gex.Commands;
using gex.Models.Lobby;
using gex.Services.Lobby;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gex.Code.Commands {

    [Command]
    public class LobbyCommand {

        private readonly ILogger<LobbyCommand> _Logger;
        private readonly LobbyManager _LobbyManager;

        public LobbyCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<LobbyCommand>>();
            _LobbyManager = services.GetRequiredService<LobbyManager>();
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

    }
}
