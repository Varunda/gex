using Dapper.ColumnMapper;
using gex.Code;
using System.Security.Permissions;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitDamage : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("dealt")]
        [ColumnMapping("damage_dealt")]
        public double DamageDealt { get; set; }

        [JsonActionLogPropertyName("taken")]
        [ColumnMapping("damage_taken")]
        public double DamageTaken { get; set; }

    }
}
