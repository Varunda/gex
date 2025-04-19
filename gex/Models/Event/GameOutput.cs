using System.Collections.Generic;

namespace gex.Models.Event {

    public class GameOutput {

        public string GameID { get; set; } = "";

        public List<GameEventUnitDef> UnitDefinitions { get; set; } = [];

        public List<GameEventWindUpdate> WindUpdates { get; set; } = [];

        public List<GameEventUnitCreated> UnitsCreated { get; set; } = [];

        public List<GameEventUnitKilled> UnitsKilled { get; set; } = [];

        public List<GameEventUnitTaken> UnitsTaken { get; set; } = [];

        public List<GameEventUnitGiven> UnitsGiven { get; set; } = [];

        public List<GameEventFactoryUnitCreated> FactoryUnitCreated { get; set; } = [];

        public List<GameEventCommanderPositionUpdate> CommanderPositionUpdates { get; set; } = [];

        public List<GameEventExtraStatUpdate> ExtraStats { get; set; } = [];

        public List<GameEventUnitTransportLoaded> TransportLoaded { get; set; } = [];

        public List<GameEventUnitTransportUnloaded> TransportUnloaded { get; set; } = [];

        public List<GameEventTeamDied> TeamDiedEvents { get; set; } = [];

        public List<GameEventUnitResources> UnitResources { get; set; } = [];

        public List<GameEventUnitDamage> UnitDamage { get; set; } = [];

		public List<GameEventUnitPosition> UnitPosition { get; set; } = [];

        public List<GameEventTeamStats> TeamStats { get; set; } = [];

    }
}
