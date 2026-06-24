using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMatchPlayerLeft {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";
 
        [ColumnMapping("player_id")]
        public byte PlayerID { get; set; }

        [ColumnMapping("reason")]
        public byte Reason { get; set; }

        [ColumnMapping("game_time")]
        public float GameTime { get; set; }

        [ColumnMapping("index")]
        public int Index { get; set; }

    }
}
