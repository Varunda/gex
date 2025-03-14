using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitCreated : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("unit_x")]
        [ColumnMapping("unit_x")]
        public decimal UnitX { get; set; }

        [JsonActionLogPropertyName("unit_y")]
        [ColumnMapping("unit_y")]
        public decimal UnitY { get; set; }

        [JsonActionLogPropertyName("unit_z")]
        [ColumnMapping("unit_z")]
        public decimal UnitZ { get; set; }

    }
}
