using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MapEngineUsage {

        [ColumnMapping("engine")]
        public string Engine { get; set; } = "";

        [ColumnMapping("map")]
        public string Map { get; set; } = "";

        [ColumnMapping("last_used")]
        public DateTime LastUsed { get; set; }

        [ColumnMapping("deleted_on")]
        public DateTime? DeletedOn { get; set; }

    }
}
