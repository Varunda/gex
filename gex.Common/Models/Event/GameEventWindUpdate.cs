using Dapper.ColumnMapper;
using gex.Common.Code;

namespace gex.Common.Models.Event {

    [DapperColumnsMapped]
    public class GameEventWindUpdate : GameEvent {

        [JsonActionLogPropertyName("value")]
        [ColumnMapping("value")]
        public double Value { get; set; }

    }
}
