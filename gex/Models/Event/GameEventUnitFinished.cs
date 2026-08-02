using gex.Code;

namespace gex.Models.Event {

    /// <summary>
    ///     event for when a unit has finished being created. this event is not stored in the DB, and is instead
    ///     used to populate the value of <see cref="GameEventUnitCreated.Completed"/> when parsed
    /// </summary>
    public class GameEventUnitFinished : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        public int TeamID { get; set; }

    }
}
