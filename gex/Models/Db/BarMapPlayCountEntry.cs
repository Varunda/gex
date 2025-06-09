using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Constants;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMapPlayCountEntry {

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("map")]
        public string Map { get; set; } = "";

        [ColumnMapping("count")]
        public int Count { get; set; }

    }
}
