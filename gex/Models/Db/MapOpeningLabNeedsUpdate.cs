using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MapOpeningLabNeedsUpdate {

        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("gamemode")]
        public int Gamemode { get; set; }

        [ColumnMapping("last_dirtied")]
        public DateTime LastDirtied { get; set; }

    }
}
