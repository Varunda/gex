using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class SkillHistogramEntry {

        [ColumnMapping("skill")]
        public int SkillLowerBound { get; set; }

        [ColumnMapping("count")]
        public int PlayerCount { get; set; }

    }
}
