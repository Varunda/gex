using System.Collections.Generic;

namespace gex.Models.Bar {

    public class BarLeaderboard {

        public string Gamemode { get; set; } = "";

        public List<BarLeaderboardPlayer> Players { get; set; } = [];

    }
}
