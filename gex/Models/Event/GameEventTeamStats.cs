using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventTeamStats : GameEvent {

        [JsonPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonPropertyName("metalProduced")]
        [ColumnMapping("metal_produced")]
        public decimal MetalProduced { get; set; }

        [JsonPropertyName("metalUsed")]
        [ColumnMapping("metal_used")]
        public decimal MetalUsed { get; set; }

        [JsonPropertyName("metalExcess")]
        [ColumnMapping("metal_excess")]
        public decimal MetalExcess { get; set; }

        [JsonPropertyName("metalSent")]
        [ColumnMapping("metal_sent")]
        public decimal MetalSent { get; set; }

        [JsonPropertyName("metalReceived")]
        [ColumnMapping("metal_received")]
        public decimal MetalReceived { get; set; }

        [JsonPropertyName("energyProduced")]
        [ColumnMapping("energy_produced")]
        public decimal EnergyProduced { get; set; }

        [JsonPropertyName("energyReceived")]
        [ColumnMapping("energy_received")]
        public decimal EnergyReceived { get; set; }

        [JsonPropertyName("energySent")]
        [ColumnMapping("energy_sent")]
        public decimal EnergySent { get; set; }

        [JsonPropertyName("energyExcess")]
        [ColumnMapping("energy_excess")]
        public decimal EnergyExcess { get; set; }

        [JsonPropertyName("energyUsed")]
        [ColumnMapping("energy_used")]
        public decimal EnergyUsed { get; set; }

        [JsonPropertyName("damageDealt")]
        [ColumnMapping("damage_dealt")]
        public decimal DamageDealt { get; set; }

        [JsonPropertyName("damageReceived")]
        [ColumnMapping("damage_received")]
        public decimal DamageReceived { get; set; }

        [JsonPropertyName("unitsProduced")]
        [ColumnMapping("units_produced")]
        public int UnitsProduced { get; set; }

        [JsonPropertyName("unitsKilled")]
        [ColumnMapping("units_killed")]
        public int UnitsKilled { get; set; }

        [JsonPropertyName("unitsSent")]
        [ColumnMapping("units_sent")]
        public int UnitsSent { get; set; }

        [JsonPropertyName("unitsCaptured")]
        [ColumnMapping("units_captured")]
        public decimal UnitsCaptured { get; set; }

        [JsonPropertyName("unitsOutCaptured")]
        [ColumnMapping("units_out_captured")]
        public int UnitsOutCaptured { get; set; }

    }
}
