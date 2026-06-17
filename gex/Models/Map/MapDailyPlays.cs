using Dapper.ColumnMapper;
using gex.Code;
using gex.Models.Bar;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapDailyPlays {

        /// <summary>
        ///     the <see cref="BarMap.FileName"/> of the map
        /// </summary>
        [ColumnMapping("map_name")]
        public string MapName { get; set; } = "";

        /// <summary>
        ///     UTC day this count takes place on
        /// </summary>
        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        /// <summary>
        ///     how many games in total were played
        /// </summary>
        [ColumnMapping("count")]
        public int Count { get; set; }

    }

}
