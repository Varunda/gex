using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    /// <summary>
    ///     event for when a unit is created. this fires when the unit starts being constructed, not when it's finished
    /// </summary>
    [DapperColumnsMapped]
    public class GameEventUnitCreated : GameEvent {

        /// <summary>
        ///     unique ID of the unit
        /// </summary>
        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("unit_x")]
        [ColumnMapping("unit_x")]
        public decimal UnitX { get; set; }

        [JsonActionLogPropertyName("unit_y")]
        [ColumnMapping("unit_y")]
        public decimal UnitY { get; set; }

        [JsonActionLogPropertyName("unit_z")]
        [ColumnMapping("unit_z")]
        public decimal UnitZ { get; set; }

        /// <summary>
        ///     0: south, north: 3.1431, west: 1.5707, east: -1.5707
        /// </summary>
        [JsonActionLogPropertyName("yaw")]
        [ColumnMapping("rotation")]
        public double Rotation { get; set; }

    }
}
