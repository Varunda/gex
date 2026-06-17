using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using gex.Models.Db;
using System;

namespace gex.Models.MapStats {

    [DapperColumnsMapped]
    public class MapStatsDailyOpeningLab {

        /// <summary>
        ///     filename of the map (this is <see cref="BarMatch.MapName"/>, not <see cref="BarMatch.Map"/>)
        /// </summary>
        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        /// <summary>
        ///     gamemode these stats are for, see <see cref="BarGamemode"/>
        /// </summary>
        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; }

        /// <summary>
        ///     UTC midnight day these stats are for (based on start time of the match)
        /// </summary>
        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        /// <summary>
        ///     definition name of the opening lab, hardcoded what is included
        /// </summary>
        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        /// <summary>
        ///     how many times this lab was the first one made
        /// </summary>
        [ColumnMapping("count")]
        public int Count { get; set; }

        /// <summary>
        ///     how many times this lab was on an ally team that one 
        /// </summary>
        [ColumnMapping("wins")]
        public int Wins { get; set; }

        /// <summary>
        ///     when this data was generated
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
