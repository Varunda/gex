using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [Obsolete]
    [DapperColumnsMapped]
    public class UserUnitsMadeNeedsUpdate {

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("day")]
        public DateTime Day { get; set; }

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("last_dirtied")]
        public DateTime LastDirtied { get; set; }

    }
}
