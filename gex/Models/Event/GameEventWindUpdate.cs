using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventWindUpdate : GameEvent {

        [JsonActionLogPropertyName("value")]
        [ColumnMapping("value")]
        public double Value { get; set; }

    }
}
