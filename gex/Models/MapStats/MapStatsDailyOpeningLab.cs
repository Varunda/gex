using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsDailyOpeningLab {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; }

        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        [ColumnMapping("count")]
        public int Count { get; set; }

        [ColumnMapping("wins")]
        public int Wins { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
