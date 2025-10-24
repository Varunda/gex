using gex.Common.Code.Constants;
using System.Collections.Generic;

namespace gex.Common.Models.Lobby {

    public class LobbyBattle {

        public int BattleID { get; set; }

        public int Type { get; set; }

        public int NatType { get; set; }

        public string FounderUsername { get; set; } = "";

        public string IP { get; set; } = "";

        public int Port { get; set; }

        public int MaxPlayers { get; set; }

        /// <summary>
        ///     number of spectators. comes from UPDATEBATTLEINFO, not BATTLEOPENED
        /// </summary>
        public int SpectatorCount { get; set; }

        public bool Passworded { get; set; }

        public bool Locked { get; set; }

        public int Rank { get; set; }

        public uint MapHash { get; set; }

        public string Engine { get; set; } = "";

        public string EngineVersion { get; set; } = "";

        public string Map { get; set; } = "";

        public string Title { get; set; } = "";

        public string GameName { get; set; } = "";

        public string Channel { get; set; } = "";

        /// <summary>
        ///     number of teams. comes from s.battle.teams, not BATTLEOPENED
        /// </summary>
        public int TeamCount { get; set; }

        /// <summary>
        ///     number of players on each time. comes from s.battle.teams, not BATTLEOPENED
        /// </summary>
        public int TeamSize { get; set; }

        public HashSet<long> Users { get; set; } = [];

        public LobbyBattleStatus? BattleStatus { get; set; } = null;

        public byte Gamemode {
            get {
                if (TeamCount == 2 && TeamSize == 1) {
                    return BarGamemode.DUEL;
                } else if (TeamCount == 2 && TeamSize <= 5) {
                    return BarGamemode.SMALL_TEAM;
                } else if (TeamCount == 2 && TeamSize <= 8) {
                    return BarGamemode.LARGE_TEAM;
                } else if (TeamCount > 2 && TeamSize == 1) {
                    return BarGamemode.FFA;
                } else if (TeamCount > 2 && TeamSize >= 2) {
                    return BarGamemode.TEAM_FFA;
                }

                return 0;
            }
        }

        /// <summary>
        ///     get how many users are players (not spectators!) in the lobby
        /// </summary>
        public int PlayerCount {
            get {
                return BattleStatus?.Clients.Count ?? Users.Count;
            }
        }

    }
}
