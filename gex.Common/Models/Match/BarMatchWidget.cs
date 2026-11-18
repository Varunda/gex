using Dapper.ColumnMapper;
using gex.Common.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Match {

    [DapperColumnsMapped]
    public class BarMatchPlayerWidget {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("user_id")]
        public long UserID { get; set; } = 0;

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

        [ColumnMapping("state")]
        public int State { get; set; } = 0;

    }
}
