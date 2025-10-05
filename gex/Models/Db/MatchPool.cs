using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MatchPool {

        [ColumnMapping("id")]
        public long ID { get; set; }

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        [ColumnMapping("created_by_id")]
        public long CreatedByID { get; set; }

    }
}
