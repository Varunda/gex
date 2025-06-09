using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class SkillHistogramEntry {

        [ColumnMapping("skill")]
        public int SkillLowerBound { get; set; }

        [ColumnMapping("count")]
        public int PlayerCount { get; set; }

    }
}
