using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class UserUnitsMadeLeaderboardEntry {

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("username")]
        public string Username { get; set; } = "";

        [ColumnMapping("count")]
        public long Count { get; set; }

    }
}
