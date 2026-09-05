using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

namespace gex.Common.Models.User {

    [DapperColumnsMapped]
    public class BarUser {

        [ColumnMapping("id")]
        public long UserID { get; set; }

        [ColumnMapping("username")]
        public string Username { get; set; } = "";

        [ColumnMapping("last_updated")]
        public DateTime LastUpdated { get; set; }

        [ColumnMapping("country_code")]
        public string? CountryCode { get; set; }

    }
}
