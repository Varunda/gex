using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsNeedsUpdate {

        /// <summary>
        ///     filename of the map
        /// </summary>
        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        /// <summary>
        ///     UTC midnight day these stats need updating for
        /// </summary>
        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        /// <summary>
        ///     gamemode of the map that needs updating
        /// </summary>
        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; }

        /// <summary>
        ///     when a fully processed match was last simmed
        /// </summary>
        [ColumnMapping("last_dirtied")]
        public DateTime LastDirtied { get; set; } = DateTime.UtcNow;

    }
}
