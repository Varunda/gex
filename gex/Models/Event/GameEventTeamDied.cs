using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    public class GameEventTeamDied : GameEvent {

        [JsonActionLogPropertyName("teamID")]
        public int TeamID { get; set; }

    }
}
