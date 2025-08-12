using gex.Models.Lobby;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace gex.Services.Lobby {

    public class LobbyManager {

        private readonly ILogger<LobbyManager> _Logger;

        private static ConcurrentDictionary<int, LobbyBattle> _Battles = [];

        /// <summary>
        ///     dictionary of users online
        /// </summary>
        private static ConcurrentDictionary<long, LobbyUser> _Users = [];

        /// <summary>
        ///     maps usernames to user IDs
        /// </summary>
        private static ConcurrentDictionary<string, long> _UsernameMapping = [];

        /// <summary>
        ///     maps users to battle IDs
        /// </summary>
        private static ConcurrentDictionary<long, int> _UserBattleMapping = [];

        public LobbyManager(ILogger<LobbyManager> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     clear all state management. used when disconnecting from the lobby
        /// </summary>
        public void Clear() {
            _Battles.Clear();
            _Users.Clear();
            _UsernameMapping.Clear();
            _UserBattleMapping.Clear();
        }

        /// <summary>
        ///     get a specific <see cref="LobbyBattle"/>
        /// </summary>
        /// <param name="battleID"></param>
        /// <returns></returns>
        public LobbyBattle? GetBattle(int battleID) {
            lock (_Battles) {
                _Battles.TryGetValue(battleID, out LobbyBattle? battle);
                return battle;
            }
        }

        /// <summary>
        ///     get a newly allocated list of all <see cref="LobbyBattle"/>s
        /// </summary>
        /// <returns></returns>
        public List<LobbyBattle> GetBattles() {
            lock (_Battles) {
                List<LobbyBattle> battles = new List<LobbyBattle>(_Battles.Values);
                return battles;
            }
        }

        /// <summary>
        ///     add a new <see cref="LobbyBattle"/> that is being tracked
        /// </summary>
        /// <param name="battle"></param>
        public void AddBattle(LobbyBattle battle) {
            lock (_Battles) {
                if (_Battles.TryGetValue(battle.BattleID, out LobbyBattle? existing) == true) {
                    _Logger.LogWarning($"duplicate battle? [battleID={battle.BattleID}]");
                }

                _Battles.AddOrUpdate(battle.BattleID, battle, (key, value) => battle);
            }
        }

        /// <summary>
        ///     remove a <see cref="LobbyBattle"/>
        /// </summary>
        /// <param name="battleID"></param>
        public void RemoveBattle(int battleID) {
            lock (_Battles) {
                if (_Battles.TryRemove(battleID, out LobbyBattle? battle) == false) {
                    _Logger.LogWarning($"failed to remove battle [battleID={battleID}]");
                } else {
                    lock (_UserBattleMapping) {
                        foreach (long userID in battle.Users) {
                            _UserBattleMapping.TryRemove(userID, out int _);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     add a <see cref="LobbyUser"/> to a <see cref="LobbyBattle"/>
        /// </summary>
        /// <param name="battleID"></param>
        /// <param name="username"></param>
        public void AddUserToBattle(int battleID, string username) {
            LobbyUser? user = GetUser(username);
            if (user == null) {
                _Logger.LogWarning($"cannot add user to battle user by username does not exist [battleID={battleID}] [username={username}]");
                return;
            }

            lock (_UserBattleMapping) {
                _UserBattleMapping.AddOrUpdate(user.UserID, battleID, (key, value) => battleID);
            }

            LobbyBattle? battle = GetBattle(battleID);
            if (battle == null) {
                _Logger.LogWarning($"cannot fully add user to battle, cannot get battle [battleID={battle}] [username={username}]");
            } else {
                battle.Users.Add(user.UserID);
            }
        }

        /// <summary>
        ///     remove a <see cref="LobbyUser"/> from a <see cref="LobbyBattle"/>
        /// </summary>
        /// <param name="username"></param>
        /// <param name="battleID"></param>
        public void RemoveUserFromBattle(string username, int battleID) {
            LobbyUser? user = GetUser(username);
            if (user == null) {
                _Logger.LogWarning($"cannot remove user from battle, username does not exist [username={username}]");
                return;
            }

            lock (_UserBattleMapping) {
                _UserBattleMapping.TryRemove(user.UserID, out int _);
            }

            LobbyBattle? battle = GetBattle(battleID);
            if (battle == null) {
                _Logger.LogWarning($"cannot fully remove user from battle, cannot get battle [battleID={battle}] [username={username}]");
            } else {
                battle.Users.Remove(user.UserID);
            }
        }

        /// <summary>
        ///     update a <see cref="LobbyBattle"/> with new parameters
        /// </summary>
        /// <param name="battleID"></param>
        /// <param name="battle"></param>
        public void UpdateBattle(int battleID, LobbyBattle battle) {
            lock (_Battles) {
                _Battles.AddOrUpdate(battleID, battle, (key, value) => battle);
            }
        }

        /// <summary>
        ///     get a specific <see cref="LobbyUser"/>
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public LobbyUser? GetUser(string username) {
            if (_UsernameMapping.TryGetValue(username, out long userID) == false) {
                return null;
            }
            return GetUser(userID);
        }

        /// <summary>
        ///     get all <see cref="LobbyUser"/>s. this allocates a new listj
        /// </summary>
        /// <returns></returns>
        public List<LobbyUser> GetUsers() {
            lock (_Users) {
                List<LobbyUser> users = new List<LobbyUser>(_Users.Values);
                return users;
            }
        }

        /// <summary>
        ///     get a specific <see cref="LobbyUser"/>
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public LobbyUser? GetUser(long userID) {
            _Users.TryGetValue(userID, out LobbyUser? user);
            return user;
        }

        /// <summary>
        ///     add a new <see cref="LobbyUser"/> being tracked
        /// </summary>
        /// <param name="user"></param>
        public void AddUser(LobbyUser user) {
            lock (_Users) {
                if (_Users.ContainsKey(user.UserID) == true) {
                    _Logger.LogWarning($"duplicate lobby user? [userID={user.UserID}]");
                }

                _Users.AddOrUpdate(user.UserID, user, (key, value) => user);

                _UsernameMapping.AddOrUpdate(user.Username, user.UserID, (key, value) => user.UserID);
            }
        }

        /// <summary>
        ///     remove a <see cref="LobbyUser"/> as being tracked
        /// </summary>
        /// <param name="username"></param>
        public void RemoveUser(string username) {
            if (_UsernameMapping.TryGetValue(username, out long userID) == false) {
                _Logger.LogWarning($"failed to get username to userID mapping for removing user [username={username}]");
                return;
            }

            if (_Users.TryRemove(userID, out LobbyUser? _) == false) {
                _Logger.LogWarning($"failed to remove user from online users [userID={userID}]");
            }

            if (_UserBattleMapping.TryRemove(userID, out int battleID)) {
                LobbyBattle? battle = GetBattle(battleID);
                if (battle == null) {
                    _Logger.LogWarning($"failed to find battle to remove user from [battleID={battleID}] [username={username}]");
                } else {
                    battle.Users.Remove(userID);
                }
            }
        }

        /// <summary>
        ///     update the state of a <see cref="LobbyUser"/> based on name
        /// </summary>
        /// <param name="username"></param>
        /// <param name="user"></param>
        public void UpdateUserStatus(string username, LobbyUser user) {
            if (_UsernameMapping.TryGetValue(username, out long userID) == false) {
                _Logger.LogWarning($"failed to get username to userID mapping for updating status [username={username}]");
                return;
            }

            if (_Users.TryGetValue(userID, out LobbyUser? oldUser) == false || oldUser == null) {
                _Users.AddOrUpdate(userID, user, (key, value) => user);
            } else {
                _Users.TryUpdate(userID, user, oldUser);
            }
        }

    }
}
