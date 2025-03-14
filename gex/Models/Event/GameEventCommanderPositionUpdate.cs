using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventCommanderPositionUpdate : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("posX")]
        [ColumnMapping("unit_x")]
        public double UnitX { get; set; }

        [JsonActionLogPropertyName("posY")]
        [ColumnMapping("unit_y")]
        public double UnitY { get; set; }

        [JsonActionLogPropertyName("posZ")]
        [ColumnMapping("unit_z")]
        public double UnitZ { get; set; }

    }
}
