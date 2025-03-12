using System.Text.Json.Serialization;

namespace gex.Models.Event {

    public class GameEventArmyValueUpdate : GameEvent {

        [JsonPropertyName("teamID")]
        public int TeamID { get; set; }

        [JsonPropertyName("value")]
        public long Value { get; set; }

    }
}
