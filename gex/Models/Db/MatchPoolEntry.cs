using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MatchPoolEntry {

        [ColumnMapping("pool_id")]
        public long PoolID { get; set; }

        [ColumnMapping("match_id")]
        public string MatchID { get; set; } = "";

        [ColumnMapping("added_by_id")]
        public long AddedByID { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
