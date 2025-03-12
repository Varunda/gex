using System.Numerics;

namespace gex.Models.Db {

    public class BarMatchPlayer {

        /// <summary>
        ///     ID of the game
        /// </summary>
        public string GameID { get; set; } = "";

        /// <summary>
        ///     ID of the player within the match
        /// </summary>
        public long PlayerID { get; set; }

        /// <summary>
        ///     user ID of the player, persistent across games
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        ///     name of the player at the time of the match (this can change!)
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     team does not mean side in this context, there can be multiple teams per side. perhaps a better word
        ///     would be "army"
        /// </summary>
        public int TeamID { get; set; }

        /// <summary>
        ///     this is the ID of the team that one would think is the normal team
        /// </summary>
        public int AllyTeamID { get; set; }

        /// <summary>
        ///     name of the faction
        /// </summary>
        public string Faction { get; set; } = "";

        /// <summary>
        ///     where this user started
        /// </summary>
        public Vector3 StartingPosition { get; set; } = Vector3.Zero;

        /// <summary>
        ///     skill value at the time of this match
        /// </summary>
        public decimal Skill { get; set; }

        /// <summary>
        ///     uncertainty in the skill at the time of the match
        /// </summary>
        public decimal SkillUncertainty { get; set; }

        /// <summary>
        ///     32 bit representation of the color, (r &lt;&lt; 16) | (g &lt;&lt; 8) | (b &lt;&lt; 0)
        /// </summary>
        public int Color { get; set; }

        /// <summary>
        ///     ?
        /// </summary>
        public decimal Handicap { get; set; }

    }
}
