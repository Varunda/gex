using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using gex.Models.UserStats;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class UserUnitsMadeNeedsUpdate {

        /// <summary>
        ///     id of the <see cref="BarUser"/> this update is for
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        /// <summary>
        ///     UTC day to include the units made of
        /// </summary>
        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        /// <summary>
        ///     <see cref="BarMatch.MapName"/> of the map 
        /// </summary>
        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        /// <summary>
        ///     gamemode to filter the results to (see <see cref="BarGamemode"/>)
        /// </summary>
        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ColumnMapping("last_dirtied")]
        public DateTime LastDirtied { get; set; } = DateTime.UtcNow;

    }
}
