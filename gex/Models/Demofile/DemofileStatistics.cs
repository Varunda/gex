using System.Collections.Generic;

namespace gex.Models.Demofile {

    public class DemofileStatistics {

        public List<byte> WinningAllyTeamIDs { get; set; } = [];

        public List<DemofilePlayerStats> PlayerStats { get; set; } = [];

    }
}
