using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMatchChatMessage {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("game_timestamp")]
        public float GameTimestamp { get; set; }

        [ColumnMapping("size")]
        public byte Size { get; set; }

        [ColumnMapping("from_id")]
        public byte FromId { get; set; }

        [ColumnMapping("to_id")]
        public byte ToId { get; set; }

        [ColumnMapping("message")]
        public string Message { get; set; } = "";

    }
}
