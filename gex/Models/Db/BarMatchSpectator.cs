using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMatchSpectator {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        /// <summary>
        ///     can be null if the spectator joined late and the username found
        ///     is not for a user in bar_user
        /// </summary>
        [ColumnMapping("user_id")]
        public long? UserID { get; set; }

        [ColumnMapping("player_id")]
        public int PlayerID { get; set; }

        [ColumnMapping("user_name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     indicates that no actual UserID was given, and instead this is a guess based on username and
        ///     the current usernames at time of parse (NOT time of match!)
        /// </summary>
        [ColumnMapping("user_id_can_be_wrong")]
        public bool UserIDCanBeWrong { get; set; }

    }
}
