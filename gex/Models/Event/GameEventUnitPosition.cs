using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitPosition : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("x")]
        [ColumnMapping("x")]
        public double X { get; set; }

        [JsonActionLogPropertyName("y")]
        [ColumnMapping("y")]
        public double Y { get; set; }

        [JsonActionLogPropertyName("z")]
        [ColumnMapping("z")]
        public double Z { get; set; }

    }
}
