using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventFactoryUnitCreated : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("factoryID")]
        [ColumnMapping("factory_unit_id")]
        public int FactoryUnitID { get; set; }

        [JsonActionLogPropertyName("factoryDefID")]
        [ColumnMapping("factory_definition_id")]
        public int FactoryDefinitionID { get; set; }

    }
}
