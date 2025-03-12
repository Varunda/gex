using System.Text.Json.Serialization;

namespace gex.Models.Event {

    public class GameEventWindUpdate : GameEvent {

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

    }
}
