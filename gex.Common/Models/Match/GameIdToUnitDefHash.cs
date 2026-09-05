using Dapper.ColumnMapper;
using gex.Common.Code;

namespace gex.Common.Models.Match {

    [DapperColumnsMapped]
    public class GameIdToUnitDefHash {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

    }
}
