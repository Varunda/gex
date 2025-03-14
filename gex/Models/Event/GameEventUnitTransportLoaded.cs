using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitTransportLoaded : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("transportUnitID")]
        [ColumnMapping("transport_unit_id")]
        public int TransportUnitID { get; set; }

        [JsonActionLogPropertyName("transportDefID")]
        [ColumnMapping("transport_team_id")]
        public int TransportTeamID { get; set; }

        [JsonActionLogPropertyName("unitX")]
        [ColumnMapping("unit_x")]
        public double UnitX { get; set; }

        [JsonActionLogPropertyName("unitY")]
        [ColumnMapping("unit_y")]
        public double UnitY { get; set; }

        [JsonActionLogPropertyName("unitZ")]
        [ColumnMapping("unit_z")]
        public double UnitZ { get; set; }

    }
}
