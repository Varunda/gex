using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Constants;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsByGamemode {

        [ColumnMapping("map_file_name")]
        public string MapFileName { get; set; } = "";

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        [ColumnMapping("play_count_day")]
        public int PlayCountDay { get; set; }

        [ColumnMapping("play_count_week")]
        public int PlayCountWeek { get; set; }

        [ColumnMapping("play_count_month")]
        public int PlayCountMonth { get; set; }

        [ColumnMapping("play_count_all_time")]
        public int PlayCountAllTime { get; set; }

        [ColumnMapping("duration_average_ms")]
        public double DurationAverageMs { get; set; }

        [ColumnMapping("duration_median_ms")]
        public double DurationMedianMs { get; set; }

    }

}
