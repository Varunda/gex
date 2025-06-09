using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Constants;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsOpeningLab {

        [ColumnMapping("map_file_name")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("map_file_name")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("def_name")]
        public string DefName { get; set; } = "";

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        [ColumnMapping("count_total")]
        public int CountTotal { get; set; }

        [ColumnMapping("win_total")]
        public int WinTotal { get; set; }

        [ColumnMapping("count_month")]
        public int CountMonth { get; set; }

        [ColumnMapping("win_month")]
        public int WinMonth { get; set; }

        [ColumnMapping("count_week")]
        public int CountWeek { get; set; }

        [ColumnMapping("win_week")]
        public int WinWeek { get; set; }

        [ColumnMapping("count_day")]
        public int CountDay { get; set; }

        [ColumnMapping("win_day")]
        public int WinDay { get; set; }

    }
}
