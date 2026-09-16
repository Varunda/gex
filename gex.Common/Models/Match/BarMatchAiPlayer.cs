using Dapper.ColumnMapper;
using gex.Common.Code;

namespace gex.Common.Models.Match {

    [DapperColumnsMapped]
    public class BarMatchAiPlayer {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("ai_id")]
        public int AiID { get; set; }

        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

    }
}
