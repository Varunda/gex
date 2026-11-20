using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

namespace gex.Models.Bar {

    [DapperColumnsMapped]
    public class BarTweakEntryNeedsUpdate {

        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        [ColumnMapping("last_dirtied")]
        public DateTime LastDirtied { get; set; }

    }
}
