using System.Text.Json.Serialization;

namespace gex.Models.Event {

    public class GameEventTeamDied : GameEvent {

        [JsonPropertyName("teamID")]
        public int TeamID { get; set; }

    }
}
