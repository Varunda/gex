using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitCreated : GameEvent {

        [JsonPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonPropertyName("unit_x")]
        [ColumnMapping("unit_x")]
        public decimal UnitX { get; set; }

        [JsonPropertyName("unit_y")]
        [ColumnMapping("unit_y")]
        public decimal UnitY { get; set; }

        [JsonPropertyName("unit_z")]
        [ColumnMapping("unit_z")]
        public decimal UnitZ { get; set; }

    }
}
