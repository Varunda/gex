using Dapper.ColumnMapper;
using gex.Common.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Bar {

    [DapperColumnsMapped]
    public class BarWidgetEntry {

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [ColumnMapping("author")]
        public string Author { get; set; } = "";

        [ColumnMapping("description")]
        public string Description { get; set; } = "";

    }
}
