using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitKilled : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("attackerID")]
        [ColumnMapping("attacker_id")]
        public int? AttackerID { get; set; }

        [JsonActionLogPropertyName("attackerDefID")]
        [ColumnMapping("attacker_definition_id")]
        public int? AttackerDefinitionID { get; set; }

        [JsonActionLogPropertyName("attackerTeam")]
        [ColumnMapping("attacker_team")]
        public int? AttackerTeam { get; set; }

        [JsonActionLogPropertyName("weaponDefID")]
        [ColumnMapping("weapon_definition_id")]
        public int WeaponDefinitionID { get; set; }

        [JsonActionLogPropertyName("killed_x")]
        [ColumnMapping("killed_x")]
        public decimal KilledX { get; set; }

        [JsonActionLogPropertyName("killed_y")]
        [ColumnMapping("killed_y")]
        public decimal KilledY { get; set; }

        [JsonActionLogPropertyName("killed_z")]
        [ColumnMapping("killed_z")]
        public decimal KilledZ { get; set; }

        [JsonActionLogPropertyName("attacker_x")]
        [ColumnMapping("attacker_x")]
        public decimal? AttackerX { get; set; }

        [JsonActionLogPropertyName("attacker_y")]
        [ColumnMapping("attacker_y")]
        public decimal? AttackerY { get; set; }

        [JsonActionLogPropertyName("attacker_z")]
        [ColumnMapping("attacker_z")]
        public decimal? AttackerZ { get; set; }

    }
}
