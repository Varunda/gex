using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

namespace gex.Common.Models.User {

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
