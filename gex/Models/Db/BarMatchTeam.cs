using System.Numerics;

namespace gex.Models.Db {

    public class BarMatchTeam {

        /// <summary>
        ///     ID of the <see cref="BarMatch"/>
        /// </summary>
        public string GameID { get; set; } = "";

        /// <summary>
        ///     unique ID of the <see cref="BarMatchTeam"/>
        /// </summary>
        public int TeamID { get; set; }

        /// <summary>
        ///     ID of the <see cref="BarMatchAllyTeam"/> this team is on
        /// </summary>
        public int AllyTeamID { get; set; }

        /// <summary>
        ///     ID of the <see cref="BarMatchPlayer"/> that led this team
        /// </summary>
        public int TeamLeaderID { get; set; }

        /// <summary>
        ///     string of the faction
        /// </summary>
        public string Faction { get; set; } = "";

        /// <summary>
        ///     32 bit color of the team. xxxxxxxx_rrrrrrrrr_gggggggg_bbbbbbbb
        /// </summary>
        public int Color { get; set; }

        /// <summary>
        ///     handicap that gives extra resources. probably just an int
        /// </summary>
        public float Handicap { get; set; }

        /// <summary>
        ///     where the commander for this team started
        /// </summary>
        public Vector3 StartingPosition { get; set; } = Vector3.Zero;

        /// <summary>
        ///     the position on the map the player started at, e.x. P1, P4, etc.
        ///     is null if the map does not have this info
        /// </summary>
        public string? StartSpot { get; set; }

        /// <summary>
        ///     name of the start spot, e.x. tech, front, sea, etc.
        ///     is null if the map does not have this info
        /// </summary>
        public string? StartSpotLabel { get; set; }

    }
}
