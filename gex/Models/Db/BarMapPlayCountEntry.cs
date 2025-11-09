using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMapPlayCountEntry {

        [ColumnMapping("timestamp")]
        public DateTime? Timestamp { get; set; }

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("map")]
        public string Map { get; set; } = "";

        [ColumnMapping("count")]
        public int Count { get; set; }

    }
}
