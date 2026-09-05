using Dapper.ColumnMapper;
using gex.Common.Code;

namespace gex.Common.Models.Match {

    [DapperColumnsMapped]
    public class BarMatchPlayer {

        /// <summary>
        ///     ID of the game
        /// </summary>
        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        /// <summary>
        ///     ID of the player within the match
        /// </summary>
        [ColumnMapping("player_id")]
        public int PlayerID { get; set; }

        /// <summary>
        ///     user ID of the player, persistent across games
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        /// <summary>
        ///     name of the player at the time of the match (this can change!)
        /// </summary>
        [ColumnMapping("user_name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     ID of the <see cref="BarMatchTeam"/> this player is on
        /// </summary>
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        /// <summary>
        ///     ID of the <see cref="BarMatchAllyTeam"/> this player is on
        /// </summary>
        [ColumnMapping("ally_team_id")]
        public int AllyTeamID { get; set; }

        /// <summary>
        ///     skill value at the time of this match
        /// </summary>
        [ColumnMapping("skill")]
        public double Skill { get; set; }

        /// <summary>
        ///     uncertainty in the skill at the time of the match
        /// </summary>
        [ColumnMapping("skill_uncertainty")]
        public double SkillUncertainty { get; set; }

        /// <summary>
        ///     2 letter country code. only used in parsing, not returned from API
        /// </summary>
        [ColumnMapping("country_code")]
        public string? CountryCode { get; set; }

    }
}
