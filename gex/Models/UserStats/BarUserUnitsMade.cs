using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class BarUserUnitsMade {

        /// <summary>
        ///     ID of the user these created for stats are for
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("date")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     definition name of the unit
        /// </summary>
        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        /// <summary>
        ///     this is the actual unit name, not the definition name
        /// </summary>
        [ColumnMapping("unit_name")]
        public string UnitName { get; set; } = "";

        /// <summary>
        ///     how many times this unit has been created
        /// </summary>
        [ColumnMapping("count")]
        public long Count { get; set; }

    }
}
