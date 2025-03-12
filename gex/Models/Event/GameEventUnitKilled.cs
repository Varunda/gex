using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitKilled : GameEvent {

        [JsonPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonPropertyName("attackerID")]
        [ColumnMapping("attacker_id")]
        public int? AttackerID { get; set; }

        [JsonPropertyName("attackerDefID")]
        [ColumnMapping("attacker_definition_id")]
        public int? AttackerDefinitionID { get; set; }

        [JsonPropertyName("attackerTeam")]
        [ColumnMapping("attacker_team")]
        public int? AttackerTeam { get; set; }

        [JsonPropertyName("weaponDefID")]
        [ColumnMapping("weapon_definition_id")]
        public int WeaponDefinitionID { get; set; }

        [JsonPropertyName("killed_x")]
        [ColumnMapping("killed_x")]
        public decimal KilledX { get; set; }

        [JsonPropertyName("killed_y")]
        [ColumnMapping("killed_y")]
        public decimal KilledY { get; set; }

        [JsonPropertyName("killed_z")]
        [ColumnMapping("killed_z")]
        public decimal KilledZ { get; set; }

        [JsonPropertyName("attacker_x")]
        [ColumnMapping("attacker_x")]
        public decimal? AttackerX { get; set; }

        [JsonPropertyName("attacker_y")]
        [ColumnMapping("attacker_y")]
        public decimal? AttackerY { get; set; }

        [JsonPropertyName("attacker_z")]
        [ColumnMapping("attacker_z")]
        public decimal? AttackerZ { get; set; }

    }
}
