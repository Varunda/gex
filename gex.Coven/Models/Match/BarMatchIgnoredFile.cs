using Dapper.ColumnMapper;
using gex.Common.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Match {

    [DapperColumnsMapped]
    public class BarMatchIgnoredFile {

        [ColumnMapping("filename")]
        public string FileName { get; set; } = "";

        [ColumnMapping("reason")]
        public string Reason { get; set; } = "";

    }
}
