using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MapPriorityMod {

        [ColumnMapping("map_name")]
        public string MapName { get; set; } = "";

        [ColumnMapping("change")]
        public short Change { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
