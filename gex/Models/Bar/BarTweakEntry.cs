using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

namespace gex.Models.Bar {

    [DapperColumnsMapped]
    public class BarTweakEntry {

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [ColumnMapping("base64")]
        public string Base64 { get; set; } = "";

        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        [ColumnMapping("count")]
        public int Count { get; set; }

    }
}
