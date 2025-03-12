using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Bar {

    [DapperColumnsMapped]
    public class BarReplay {

        [ColumnMapping("id")]
        public string ID { get; set; } = "";

        [ColumnMapping("filename")]
        public string FileName { get; set; } = "";

    }
}
