using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventExtraStatUpdate : GameEvent {

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

		[JsonActionLogPropertyName("totalValue")]
		[ColumnMapping("total_value")]
		public double TotalValue { get; set; }

        [JsonActionLogPropertyName("armyValue")]
        [ColumnMapping("army_value")]
        public double ArmyValue { get; set; }

        [JsonActionLogPropertyName("defValue")]
        [ColumnMapping("defense_value")]
		public double DefenseValue { get; set; }

        [JsonActionLogPropertyName("utilValue")]
        [ColumnMapping("util_value")]
		public double UtilValue { get; set; }

        [JsonActionLogPropertyName("ecoValue")]
        [ColumnMapping("eco_value")]
		public double EcoValue { get; set; }

        [JsonActionLogPropertyName("otherValue")]
        [ColumnMapping("other_value")]
		public double OtherValue { get; set; }

        [JsonActionLogPropertyName("buildPowerAvailable")]
        [ColumnMapping("build_power_available")]
        public double BuildPowerAvailable { get; set; }

        [JsonActionLogPropertyName("buildPowerUsed")]
        [ColumnMapping("build_power_used")]
        public double BuildPowerUsed { get; set; }

        [JsonActionLogPropertyName("metalCurrent")]
        [ColumnMapping("metal_current")]
        public double MetalCurrent { get; set; }

        [JsonActionLogPropertyName("energyCurrent")]
        [ColumnMapping("energy_current")]
        public double EnergyCurrent { get; set; }

    }
}
