using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarSkillLeaderboardEntry {

        /// <summary>
        ///		gamemode the player has this skill in
        /// </summary>
        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        /// <summary>
        ///		ID of the user
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        /// <summary>
        ///		username of the user
        /// </summary>
        [ColumnMapping("username")]
        public string Username { get; set; } = "";

        /// <summary>
        ///		skill value
        /// </summary>
        [ColumnMapping("skill")]
        public float Skill { get; set; }

    }
}
