using Dapper.ColumnMapper;
using gex.Common.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Match {

    [DapperColumnsMapped]
    public class BarMatchHash {

        [ColumnMapping("id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("filename")]
        public string FileName { get; set; } = "";

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

    }
}
