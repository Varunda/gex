using System.Collections.Generic;

namespace gex.Common.Models.Lobby {

    public class LobbyWhoisResponse {

        public string Username { get; set; } = "";

        public List<string> PreviousNames { get; set; } = [];

        public long UserID { get; set; }

        public int Chevron { get; set; }

        public Dictionary<string, WhoisRating> Ratings { get; set; } = [];

    }

    public class WhoisRating {

        public string Gamemode { get; set; } = "";

        public float Rating { get; set; }

        public float Leaderboard { get; set; }

    }

}
