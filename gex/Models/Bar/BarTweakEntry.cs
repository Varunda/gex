using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

namespace gex.Models.Bar {

    /// <summary>
    ///     represents a tweak and how many times it was used in a day
    /// </summary>
    [DapperColumnsMapped]
    public class BarTweakEntry {

        /// <summary>
        ///     MD5 hash of the base64 encoded code
        /// </summary>
        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

        /// <summary>
        ///     UTC aligned day the count is for
        /// </summary>
        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        /// <summary>
        ///     if this tweak was a unit tweak or def tweak
        /// </summary>
        [ColumnMapping("type")]
        public string Type { get; set; } = "";

        /// <summary>
        ///     name of the tweak, parsed out from the lua code (if the first line is a comment, that is what is used)
        /// </summary>
        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     base64 encoded lua code
        /// </summary>
        [ColumnMapping("base64")]
        public string Base64 { get; set; } = "";

        /// <summary>
        ///     how many matches this tweak was seen in on this day
        /// </summary>
        [ColumnMapping("count")]
        public int Count { get; set; }

    }
}
