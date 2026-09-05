using System.Collections.Generic;

namespace gex.Common.Models.Demofile {

    public class DemofileStatistics {

        public List<byte> WinningAllyTeamIDs { get; set; } = [];

        public List<DemofilePlayerStats> PlayerStats { get; set; } = [];

    }
}
