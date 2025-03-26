using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventExtraStatUpdate : GameEvent {

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("armyValue")]
        [ColumnMapping("army_value")]
        public long ArmyValue { get; set; }

        [JsonActionLogPropertyName("buildPowerAvailable")]
        [ColumnMapping("build_power_available")]
        public double BuildPowerAvailable { get; set; }

        [JsonActionLogPropertyName("buildPowerUsed")]
        [ColumnMapping("build_power_used")]
        public double BuildPowerUsed { get; set; }

    }
}
