using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventTeamDied : GameEvent {

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

    }
}
