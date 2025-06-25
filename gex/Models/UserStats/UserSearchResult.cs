using Dapper.ColumnMapper;
using gex.Code;
using System;
using System.Collections.Generic;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class UserSearchResult {

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("username")]
        public string Username { get; set; } = "";

        [ColumnMapping("last_updated")]
        public DateTime LastUpdated { get; set; }

        [ColumnMapping("previous_name")]
        public string? PreviousName { get; set; } = null;

        /// <summary>
        ///     only populated if query flag is set, not loaded from DB
        /// </summary>
        public List<BarUserSkill> Skill { get; set; } = [];

    }
}
