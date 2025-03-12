using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class GameIdToUnitDefHash {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

    }
}
