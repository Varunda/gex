using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.UserStats {

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
