using System;
using System.Collections.Generic;

namespace gex.Models.Lobby {

    public class LobbyBattleStatus {

        /// <summary>
        ///     ID of the battle
        /// </summary>
        public int BattleID { get; set; }

        /// <summary>
        ///     when this battle status was captured
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     list of clients that will become players in the game
        /// </summary>
        public List<LobbyBattleStatusClient> Clients { get; set; } = [];

        /// <summary>
        ///     list of clients that are currently spectating
        /// </summary>
        public List<LobbyBattleStatusSpectator> Spectators { get; set; } = [];

        /// <summary>
        ///     bots
        /// </summary>
        public List<LobbyBattleStatusBot> Bots { get; set; } = [];

    }

    public class LobbyBattleStatusClient {

        /// <summary>
        ///     ID of the user this client is for
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        ///     username of the client
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        ///     ID of the player
        /// </summary>
        public int PlayerID { get; set; }

        /// <summary>
        ///     skill value. -1 by default if unable to find
        /// </summary>
        public double Skill { get; set; }

    }

    public class LobbyBattleStatusSpectator {

        /// <summary>
        ///     user ID of the user of the client
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        ///     username of the user of the client
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        ///     skill value. -1 by default if unable to find
        /// </summary>
        public double Skill { get; set; }

    }

    public class LobbyBattleStatusBot {

        /// <summary>
        ///     name of the bot
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     ID of the player
        /// </summary>
        public int? PlayerID { get; set; }

        /// <summary>
        ///     ID of the ally team this bot is on
        /// </summary>
        public int? AllyTeamID { get; set; }

        /// <summary>
        ///     what version of AI is this bot
        /// </summary>
        public string Version { get; set; } = "";

    }

}
