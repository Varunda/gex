using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsNeedsUpdate {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; }

        [ColumnMapping("last_dirtied")]
        public DateTime LastDirtied { get; set; } = DateTime.UtcNow;

    }
}
