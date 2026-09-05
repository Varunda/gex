using Dapper.ColumnMapper;
using gex.Common.Code;

namespace gex.Common.Models.Bar {

    [DapperColumnsMapped]
    public class BarReplay {

        [ColumnMapping("id")]
        public string ID { get; set; } = "";

        [ColumnMapping("filename")]
        public string FileName { get; set; } = "";

        [ColumnMapping("map_name")]
        public string MapName { get; set; } = "";

    }
}
