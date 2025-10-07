using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MatchPool {

        /// <summary>
        ///     unique ID of the match pool
        /// </summary>
        [ColumnMapping("id")]
        public long ID { get; set; }

        /// <summary>
        ///     name of the match pool. not unique
        /// </summary>
        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     when this match pool was created
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     id of the app account that created this match pool
        /// </summary>
        [ColumnMapping("created_by_id")]
        public long CreatedByID { get; set; }

    }
}
