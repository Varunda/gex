using System.Collections.Generic;

namespace gex.Models.Demofile {

    public class DemofileTeamStats {

        public int TeamID { get; set; }

        public int StatCount { get; set; }

        public List<DemofileTeamFrameStats> Entries { get; set; } = [];

    }
}
