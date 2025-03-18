using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventTeamStats : GameEvent {

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("metalProduced")]
        [ColumnMapping("metal_produced")]
        public decimal MetalProduced { get; set; }

        [JsonActionLogPropertyName("metalUsed")]
        [ColumnMapping("metal_used")]
        public decimal MetalUsed { get; set; }

        [JsonActionLogPropertyName("metalExcess")]
        [ColumnMapping("metal_excess")]
        public decimal MetalExcess { get; set; }

        [JsonActionLogPropertyName("metalSent")]
        [ColumnMapping("metal_sent")]
        public decimal MetalSent { get; set; }

        [JsonActionLogPropertyName("metalReceived")]
        [ColumnMapping("metal_received")]
        public decimal MetalReceived { get; set; }

        [JsonActionLogPropertyName("energyProduced")]
        [ColumnMapping("energy_produced")]
        public decimal EnergyProduced { get; set; }

        [JsonActionLogPropertyName("energyReceived")]
        [ColumnMapping("energy_received")]
        public decimal EnergyReceived { get; set; }

        [JsonActionLogPropertyName("energySent")]
        [ColumnMapping("energy_sent")]
        public decimal EnergySent { get; set; }

        [JsonActionLogPropertyName("energyExcess")]
        [ColumnMapping("energy_excess")]
        public decimal EnergyExcess { get; set; }

        [JsonActionLogPropertyName("energyUsed")]
        [ColumnMapping("energy_used")]
        public decimal EnergyUsed { get; set; }

        [JsonActionLogPropertyName("damageDealt")]
        [ColumnMapping("damage_dealt")]
        public decimal DamageDealt { get; set; }

        [JsonActionLogPropertyName("damageReceived")]
        [ColumnMapping("damage_received")]
        public decimal DamageReceived { get; set; }

        [JsonActionLogPropertyName("unitsProduced")]
        [ColumnMapping("units_produced")]
        public int UnitsProduced { get; set; }

        [JsonActionLogPropertyName("unitsKilled")]
        [ColumnMapping("units_killed")]
        public int UnitsKilled { get; set; }

        [JsonActionLogPropertyName("unitsSent")]
        [ColumnMapping("units_sent")]
        public int UnitsSent { get; set; }

        [JsonActionLogPropertyName("unitsReceived")]
        [ColumnMapping("units_received")]
        public int UnitsReceived { get; set; }

        [JsonActionLogPropertyName("unitsCaptured")]
        [ColumnMapping("units_captured")]
        public decimal UnitsCaptured { get; set; }

        [JsonActionLogPropertyName("unitsOutCaptured")]
        [ColumnMapping("units_out_captured")]
        public int UnitsOutCaptured { get; set; }

    }
}
