using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class BarMatchTeamDeath {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("team_id")]
        public byte TeamID { get; set; }

        [ColumnMapping("reason")]
        public byte Reason { get; set; }

        [ColumnMapping("game_time")]
        public float GameTime { get; set; }

    }
}
