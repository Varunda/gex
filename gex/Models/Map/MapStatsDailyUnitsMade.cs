using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsDailyUnitsMade {

        /// <summary>
        ///     ID of the user these created for stats are for
        /// </summary>
        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        /// <summary>
        ///     gamemode this data is for. see <see cref="BarGamemode"/> for the mappings
        /// </summary>
        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; }

        /// <summary>
        ///     day (aligned to UTC 00:00) this represents
        /// </summary>
        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        /// <summary>
        ///     definition name of the unit
        /// </summary>
        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        /// <summary>
        ///     how many times this unit has been created
        /// </summary>
        [ColumnMapping("count")]
        public long Count { get; set; }

        /// <summary>
        ///     when this entry was last updated
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
