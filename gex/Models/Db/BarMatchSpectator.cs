using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMatchSpectator {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("user_name")]
        public string Name { get; set; } = "";

    }
}
