using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class UserPreviousName {

        /// <summary>
        ///     user name of the user
        /// </summary>
        [ColumnMapping("user_name")]
        public string UserName { get; set; } = "";

        /// <summary>
        ///     first match the user name was seen in
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
