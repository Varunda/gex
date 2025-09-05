using gex.Models;
using gex.Models.Lobby;
using gex.Models.Queues;
using gex.Models.UserStats;
using gex.Services.Lobby;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class LobbyMessageQueueProcessor : BaseQueueProcessor<LobbyMessage> {

        private readonly LobbyManager _LobbyManager;
        private readonly BarUserRepository _UserRepository;
        private readonly ILobbyClient _LobbyClient;
        private readonly BaseQueue<BattleStatusUpdateQueueEntry> _BattleStatusUpdateQueue;

        private readonly Dictionary<string, int> _DmVelocity = [];

        private Timer? _VelocityDecreaseTimer = null;

        public LobbyMessageQueueProcessor(ILoggerFactory factory,
            BaseQueue<LobbyMessage> queue, ServiceHealthMonitor serviceHealthMonitor,
            LobbyManager lobbyManager, ILobbyClient lobbyClient,
            BarUserRepository userRepository, BaseQueue<BattleStatusUpdateQueueEntry> battleStatusUpdateQueue)
        : base("lobby_message_queue", factory, queue, serviceHealthMonitor) {

            _LobbyManager = lobbyManager;
            _LobbyClient = lobbyClient;
            _UserRepository = userRepository;
            _BattleStatusUpdateQueue = battleStatusUpdateQueue;
        }

        public override Task StartAsync(CancellationToken cancellationToken) {
            _VelocityDecreaseTimer = new Timer((object? _) => {

                HashSet<string> toRemove = [];
                foreach (KeyValuePair<string, int> entry in _DmVelocity) {
                    if ((entry.Value - 1) > 0) {
                        _DmVelocity[entry.Key] = entry.Value - 1;
                    } else {
                        toRemove.Add(entry.Key);
                    }
                }

                foreach (string rem in toRemove) {
                    _DmVelocity.Remove(rem);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            _VelocityDecreaseTimer?.Dispose();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task<bool> _ProcessQueueEntry(LobbyMessage entry, CancellationToken cancel) {
            //_Logger.LogDebug($"processing message [command={entry.Command}] [args={string.Join(" ", entry.Arguments)}]");
            // honestly the best resource is the tests to figure stuff out lol
            // https://github.com/beyond-all-reason/teiserver/blob/9cd6471e75348fa59a0eb93b64693c7ea7462cb9/test/teiserver/protocols/spring/spring_raw_test.exs#L67

            if (entry.Command == "TASSERVER") {
                _Logger.LogInformation($"lobby sent us some info! [info={entry.Arguments}]");
            } else if (entry.Command == "SAIDPRIVATE") {
                await HandleSaidPrivate(entry, cancel);
            } else if (entry.Command == "SAYPRIVATE") {
                // yep, that PM was sure sent :)
            } else if (entry.Command == "s.system.disconnect") {
                _Logger.LogWarning($"lobby disconnected from us! [reason={entry.Arguments}]");
                await _LobbyClient.Disconnect(cancel);
            } else if (entry.Command == "s.battle.update_lobby_title") {
                HandleBattleUpdateLobbyTitle(entry);
            } else if (entry.Command == "s.user.new_incoming_friend_request") {
                await HandleUserNewIncomingFriendRequest(entry, cancel);
            } else if (entry.Command == "s.battle.teams") {
                HandleBattleTeams(entry);
            } else if (entry.Command == "ADDUSER") {
                HandleAddUser(entry);
            } else if (entry.Command == "BATTLECLOSED") {
                HandleBattleClosed(entry);
            } else if (entry.Command == "BATTLEOPENED") {
                HandleBattleOpened(entry);
            } else if (entry.Command == "CLIENTSTATUS") {
                HandleClientStatus(entry);
            } else if (entry.Command == "JOINEDBATTLE") {
                HandleJoinedBattle(entry);
            } else if (entry.Command == "LEFTBATTLE") {
                HandleLeftBattle(entry);
            } else if (entry.Command == "MOTD") {
                _Logger.LogInformation($"MOTD line from lobby [text={entry.Arguments}]");
            } else if (entry.Command == "REMOVEUSER") {
                HandleRemoveUser(entry);
            } else if (entry.Command == "SERVERMSG") {
                string msg = entry.GetSentence() ?? "";
                _Logger.LogInformation($"message from server [msg={msg}]");
            } else if (entry.Command == "SERVERMSGBOX") {
                string msg = entry.GetSentence() ?? "";
                string? url = entry.GetSentence();
                _Logger.LogWarning($"alert message from server [msg={msg}] [url={url}]");
            } else if (entry.Command == "UPDATEBATTLEINFO") {
                HandleUpdateBattleInfo(entry);
            } else if (entry.Command == "s.user.friend_deleted") {
                //
                // ack the command, but don't do anything about it /shrug
                //
            } else {
                _Logger.LogWarning($"unhandled lobby message [command={entry.Command}] [args={entry.Arguments}]");
            }

            return true;
        }

        private async Task HandleSaidPrivate(LobbyMessage entry, CancellationToken cancel) {
            // SAIDPRIVATE userName {message}

            string? username = entry.GetWord();
            string? msg = entry.GetSentence();

            if (username == null || msg == null) {
                _Logger.LogWarning($"failed to get arguments for SAIDPRIVATE [username={username}] [msg={msg}]");
                return;
            }

            int velocity = _DmVelocity.GetValueOrDefault(username, 0);
            if (velocity >= 5) {
                _Logger.LogInformation($"ignoring DM from user due to velocity check [username={username}] [msg={msg}]");
                return;
            }

            _DmVelocity[username] = velocity + 1;

            _Logger.LogDebug($"got DM from user [username={username}] [msg={msg}]");

            // do not reply to Coordinator
            if (username == "Coordinator") {
                return;
            }

            if (msg == "/help") {
                await _LobbyClient.Write("SAYPRIVATE", $"{username} howdy! this is the Gex coordinator. "
                    + $"eventually users will be able to ask Gex to watch a game for live analysis, but not yet!", cancel);
            } else if (msg == ":3") {
                await _LobbyClient.Write("SAYPRIVATE", $"{username} :3", cancel);
            } else {
                await _LobbyClient.Write("SAYPRIVATE", $"{username} howdy! Gex does not know what that means. try /help", cancel);
            }
        }

        /// <summary>
        ///     handle a ADDUSER command, which is sent when a new user appears in the lobby
        /// </summary>
        /// <param name="entry"></param>
        private void HandleAddUser(LobbyMessage entry) {
            // 2025-08-10: this doesn't seem correct. no CPU value is sent for BAR
            // ADDUSER userName country cpu userID {lobbyID}

            string? username = entry.GetWord();
            string? country = entry.GetWord();
            string? userID = entry.GetWord();
            string? version = entry.GetSentence(); // this value is safe to be null

            if (username == null || country == null || userID == null) {
                _Logger.LogWarning($"failed to get arguments for ADDUSER [username={username}] [country={country}] [userID={userID}] [version={version}] [args={entry.Arguments}]");
                return;
            }

            if (long.TryParse(userID, out long id) == false) {
                _Logger.LogWarning($"failed to parse userID to a valid int64 in ADDUSER [userID={userID}] [args={entry.Arguments}]");
                return;
            }

            LobbyUser user = new();
            user.Username = username;
            user.UserID = id;
            user.Version = version ?? "";

            _LobbyManager.AddUser(user);
            //_Logger.LogInformation($"ADDUSER [username={username}] [userID={userID}] [country={country}] [version={version}]");
        }

        /// <summary>
        ///     handle a BATTLECLOSED command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleBattleClosed(LobbyMessage entry) {
            // BATTLECLOSED battleID

            string? battleID = entry.GetWord();

            if (battleID == null) {
                _Logger.LogWarning($"missing arguments for BATTLECLOSED [battleID={battleID}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(battleID, out int bID) == false) {
                _Logger.LogWarning($"failed to parse battleID in BATTLECLOSED to a valid int32 [battleID={battleID}]");
                return;
            }

            _LobbyManager.RemoveBattle(bID);
        }

        /// <summary>
        ///     handle a BATTLEOPENED command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleBattleOpened(LobbyMessage entry) {
            // 2025-08-10: seems like {channel} isn't sent in BAR
            // BATTLEOPENED battleID type natType founder ip port maxPlayers passworded rank mapHash
            //      {engineName} {engineVersion} {map} {title} {gameName} {channel}

            string? battleID = entry.GetWord();
            string? type = entry.GetWord();
            string? natType = entry.GetWord();
            string? founderUsername = entry.GetWord();
            string? ip = entry.GetWord();
            string? port = entry.GetWord();
            string? maxPlayers = entry.GetWord();
            string? passworded = entry.GetWord();
            string? rank = entry.GetWord();
            string? mapHash = entry.GetWord();
            string? engineName = entry.GetSentence();
            string? engineVersion = entry.GetSentence();
            string? map = entry.GetSentence();
            string? title = entry.GetSentence();
            string? gameName = entry.GetSentence();
            string? channel = entry.GetSentence(); // this is always null, isn't sent from BAR

            if (battleID == null || type == null || natType == null || founderUsername == null || ip == null || port == null || maxPlayers == null
                || passworded == null || rank == null || mapHash == null || engineName == null || engineVersion == null
                || map == null || title == null || gameName == null) {

                _Logger.LogWarning($"missing arguments for BATTLEOPENED [battleID={battleID}]");
                return;
            }

            LobbyBattle battle = new();

            if (int.TryParse(battleID, out int bID) == false) {
                _Logger.LogWarning($"failed to parse battleID to a valid int32 [battleID={battleID}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(maxPlayers, out int mp) == false) {
                _Logger.LogWarning($"failed to parse maxPlayers to a valid int32 [maxPlayers={maxPlayers}] [args={entry.Arguments}]");
                return;
            }

            battle.BattleID = bID;
            battle.FounderUsername = founderUsername;
            battle.Map = map;
            battle.MaxPlayers = mp;
            battle.Title = title;
            battle.Passworded = passworded == "1";

            _LobbyManager.AddBattle(battle);

            if (battle.Passworded == false) {
                // BATTLEOPENED is sent on login, so it can include battles that are already in progress
                LobbyUser? founder = _LobbyManager.GetUser(battle.FounderUsername);

                if (founder != null && founder.InGame == false) {
                    /*
                    _BattleStatusUpdateQueue.Queue(new BattleStatusUpdateQueueEntry() {
                        BattleID = battle.BattleID,
                        Reason = "battle_opened"
                    });
                    */
                }
            }

            //_Logger.LogInformation($"battle opened [battleID={battle.BattleID}] [map={battle.Map}] [max players={battle.MaxPlayers}] [title={battle.Title}]");
        }

        /// <summary>
        ///     handle a CLIENTSTATUS command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleClientStatus(LobbyMessage entry) {
            // CLIENTSTATUS userName status

            string? username = entry.GetWord();
            string? statestr = entry.GetWord();
            if (username == null || statestr == null) {
                _Logger.LogWarning($"failed to get arguments for CLIENTSTATUS [username={username}] [statestr={statestr}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(statestr, out int state) == false) {
                _Logger.LogWarning($"failed to parse state string into a valid int32 [statestr={statestr}] [args={entry.Arguments}]");
                return;
            }

            LobbyUser? user = _LobbyManager.GetUser(username);
            if (user == null) {
                _Logger.LogWarning($"failed to find user to update [username={username}]");
                return;
            }

            // https://springrts.com/dl/LobbyProtocol/ProtocolDescription.html#MYSTATUS:client
            user.InGame = (state & 0x01) == 0x01;
            user.Away = (state & 0x02) == 0x02;
            user.Rank = (state & 0x1C) >> 3;
            user.AccessStatus = (state & 0x20) == 0x20;
            user.IsBot = (state & 0x40) == 0x40;

            _LobbyManager.UpdateUserStatus(username, user);
        }

        /// <summary>
        ///     handle a LEFTBATTLE command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleLeftBattle(LobbyMessage entry) {
            // LEFTBATTLE battleID userName

            string? battleID = entry.GetWord();
            string? username = entry.GetWord();

            if (battleID == null || username == null) {
                _Logger.LogWarning($"missing arguments for LEFTBATTLE [battleID={battleID}] [username={username}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(battleID, out int bID) == false) {
                _Logger.LogWarning($"failed to parse battleID in LEFTBATTLE to a valid int32 [battleID={battleID}] [args={entry.Arguments}]");
                return;
            }

            _LobbyManager.RemoveUserFromBattle(username, bID);

            // only request an update if the game is opened and not running
            LobbyBattle? battle = _LobbyManager.GetBattle(bID);
            if (battle != null && battle.Passworded == false && battle.Locked == false) {
                LobbyUser? founder = _LobbyManager.GetUser(battle.FounderUsername);

                if (founder != null && founder.InGame == false) {
                    LobbyUser? userLeft = _LobbyManager.GetUser(username);
                    if (userLeft != null) {
                        // no need to fully update the battle status, the only thing that could have changed is the
                        if (battle.BattleStatus != null) {
                            battle.BattleStatus.Clients = battle.BattleStatus.Clients.Where(iter => iter.UserID != userLeft.UserID).ToList();
                            battle.BattleStatus.Spectators = battle.BattleStatus.Spectators.Where(iter => iter.UserID != userLeft.UserID).ToList();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     handle a JOINEDBATTLE command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleJoinedBattle(LobbyMessage entry) {
            // 37580 ghughes13
            string? battleID = entry.GetWord();
            string? username = entry.GetWord();

            if (battleID == null || username == null) {
                _Logger.LogWarning($"missing arguments for JOINEDBATTLE [battleID={battleID}] [username={username}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(battleID, out int bID) == false) {
                _Logger.LogWarning($"failed to parse battleID for JOINEDBATTLE to a valid int32 [battleID={battleID}] [args={entry.Arguments}]");
                return;
            }

            _LobbyManager.AddUserToBattle(bID, username);

            // only request an update if the game is opened and not running
            LobbyBattle? battle = _LobbyManager.GetBattle(bID);
            if (battle != null && battle.Passworded == false && battle.Locked == false) {
                LobbyUser? founder = _LobbyManager.GetUser(battle.FounderUsername);

                if (founder != null && founder.InGame == false) {
                    /*
                    _BattleStatusUpdateQueue.Queue(new BattleStatusUpdateQueueEntry() {
                        BattleID = bID,
                        Reason = "user_joined"
                    });
                    */
                }
            }
        }

        /// <summary>
        ///     handle a REMOVEUSER command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleRemoveUser(LobbyMessage entry) {
            // REMOVEUSER userName

            string? username = entry.GetWord();
            
            if (username == null) {
                _Logger.LogWarning($"missing arguments for REMOVEUSER [username={username}] [args={entry.Arguments}]");
                return;
            }

            _LobbyManager.RemoveUser(username);
        }

        /// <summary>
        ///     handle a UPDATEBATTLEINFO command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleUpdateBattleInfo(LobbyMessage entry) {
            // UPDATEBATTLEINFO battleID spectatorCount locked mapHash {mapName}

            string? battleID = entry.GetWord();
            string? spectatorCount = entry.GetWord();
            string? locked = entry.GetWord();
            string? mapHash = entry.GetWord();
            string? mapName = entry.GetSentence();

            if (battleID == null || spectatorCount == null || locked == null || mapHash == null || mapName == null) {
                _Logger.LogWarning($"missing arguments for UPDATEBATTLEINFO "
                    + $"[battleID={battleID}] [spectatorCount={spectatorCount}] [locked={locked}] [mapHash={mapHash}] [mapName={mapName}] "
                    + $"[args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(battleID, out int bID) == false) {
                _Logger.LogWarning($"failed to parse battleID in UPDATEBATTLEINFO to a valid int32 [battleID={battleID}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(spectatorCount, out int specCount) == false) {
                _Logger.LogWarning($"failed to parse spectatorCount in UPDATEBATTLEINFO to a valid int32 [spectatorCount={spectatorCount}] [args={entry.Arguments}]");
                return;
            }

            LobbyBattle? battle = _LobbyManager.GetBattle(bID);
            if (battle == null) {
                _Logger.LogWarning($"missing battle from lobby manager [battleID={battleID}]");
                return;
            }

            battle.Locked = ("locked" == "true");

            bool needsStatusUpdate = battle.Passworded == false
                && battle.Locked == false
                && battle.SpectatorCount != specCount;

            battle.SpectatorCount = specCount;
            battle.Map = mapName;
            _LobbyManager.UpdateBattle(bID, battle);

            LobbyUser? founder = needsStatusUpdate == true ? _LobbyManager.GetUser(battle.FounderUsername) : null;
            if (needsStatusUpdate == true && founder != null && founder.InGame == false) {
                /*
                _BattleStatusUpdateQueue.Queue(new BattleStatusUpdateQueueEntry() {
                    BattleID = battle.BattleID,
                    Reason = "spectator_count_changed"
                });
                */
            }
        }

        /// <summary>
        ///     handle a s.battle.update_lobby_title command
        /// </summary>
        /// <param name="entry"></param>
        private void HandleBattleUpdateLobbyTitle(LobbyMessage entry) {
            // https://github.com/beyond-all-reason/teiserver/blob/9cd6471e75348fa59a0eb93b64693c7ea7462cb9/documents/spring/extensions.md?plain=1#L11
            // the Chobby extensions use TABs to sep args
            // s.battle.update_lobby_title {battleID} {map}

            string? battleID = entry.GetSentence();
            string? name = entry.GetSentence();

            if (battleID == null || name == null) {
                _Logger.LogWarning($"missing arguments for s.battle.update_lobby_title [battleID={battleID}] [name={name}] [args={entry.Arguments}]");
                return;
            }

            if (int.TryParse(battleID, out int bID) == false) {
                _Logger.LogWarning($"failed to parse battleID in s.battle_update_lobby_title to a valid int32 [battleID={battleID}] [args={entry.Arguments}]");
                return;
            }

            LobbyBattle? battle = _LobbyManager.GetBattle(bID);
            if (battle == null) {
                _Logger.LogWarning($"failed to find battle to update title [battleID={bID}]");
                return;
            }

            battle.Title = name;
            _LobbyManager.UpdateBattle(bID, battle);
        }

        /// <summary>
        ///     handle a s.user.new_incoming_friend_request user_id
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="cancel"></param>
        private async Task HandleUserNewIncomingFriendRequest(LobbyMessage entry, CancellationToken cancel) {
            // s.user.new_incoming_friend_request user_id

            string? userID = entry.GetWord();

            if (userID == null) {
                _Logger.LogWarning($"missing arguements for s.user.new_incoming_friend_request [userID={userID}] [args={entry.Arguments}]");
                return;
            }

            if (long.TryParse(userID, out long uID) == false) {
                _Logger.LogWarning($"failed to parse userID in s.user.new_incoming_friend_request to a valid int64 [userID={userID}] [args={entry.Arguments}]");
                return;
            }

            string username;
            LobbyUser? user = _LobbyManager.GetUser(userID);
            if (user == null) {
                _Logger.LogWarning($"missing user from lobby manager, using DB as fallback [userID={userID}]");
                BarUser? dbUser = await _UserRepository.GetByID(uID, cancel);
                if (dbUser != null) {
                    username = dbUser.Username;
                } else {
                    _Logger.LogWarning($"failed to find user in DB, cannot accept friend reqeust [userID={userID}] [args={entry.Arguments}]");
                    return;
                }
            } else {
                username = user.Username;
            }

            // there is no response for this command
            Result<bool, string> response = await _LobbyClient.Write("ACCEPTFRIENDREQUEST", $"userName={username}", cancel);

            if (response.IsOk == false) {
                _Logger.LogWarning($"failed to accept friend request to user, request failed [error={response.Error}] [user={userID}/{username}]");
                return;
            }

            _Logger.LogInformation($"accepted friend request [userID={userID}]");
        }

        /// <summary>
        ///     handle s.battle.teams messages, which update the team count and team size in a battle
        /// </summary>
        /// <param name="entry"></param>
        private void HandleBattleTeams(LobbyMessage entry) {
            // s.battle.teams base64-json

            string? info = entry.GetWord();
            if (info == null) {
                _Logger.LogWarning($"missing arguments for s.battle.teams [info={info}]");
                return;
            }

            string b64;
            try {
                // lobby strips padding chars which c# does not like
                if (info.Length % 4 != 0) { 
                    info += new string('=', 4 - info.Length % 4);
                }
                b64 = Encoding.UTF8.GetString(Convert.FromBase64String(info));
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to parse base64 to [info={info}] [ex={ex.Message}]");
                return;
            }

            JsonObject? elem = JsonSerializer.Deserialize<JsonObject>(b64);
            if (elem == null) {
                _Logger.LogWarning($"got null json object from json string [b64={b64}]");
                return;
            }
            //_Logger.LogDebug($"updating battle team size [b64={b64}]");

            foreach (KeyValuePair<string, JsonNode?> iter in elem) {
                if (iter.Value == null) {
                    _Logger.LogWarning($"missing value for s.battle.teams [key={iter.Key}] [b64={b64}]");
                    continue;
                }

                if (int.TryParse(iter.Key, out int battleID) == false) {
                    _Logger.LogWarning($"failed to parse battleID in s.battle.teams to a valid int32 [key={iter.Key}]");
                    continue;
                }

                LobbyBattle? battle = _LobbyManager.GetBattle(battleID);
                if (battle == null) {
                    _Logger.LogWarning($"missing battle to update s.battle.teams from [battleID={battleID}]");
                    return;
                }

                int teamCount = (int)(iter.Value["nbTeams"] ?? 0);
                int teamSize = (int)(iter.Value["teamSize"] ?? 0);

                battle.TeamCount = teamCount;
                battle.TeamSize = teamSize;
                _LobbyManager.UpdateBattle(battleID, battle);
            }
        }

    }
}
