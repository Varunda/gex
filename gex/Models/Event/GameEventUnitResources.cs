using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitResources : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("metalMake")]
        [ColumnMapping("metal_made")]
        public double MetalMade { get; set; }

        [JsonActionLogPropertyName("metalUse")]
        [ColumnMapping("metal_used")]
        public double MetalUsed { get; set; }

        [JsonActionLogPropertyName("energyMake")]
        [ColumnMapping("energy_made")]
        public double EnergyMade { get; set; }

        [JsonActionLogPropertyName("energyUse")]
        [ColumnMapping("energy_used")]
        public double EnergyUsed { get; set; }

    }
}
