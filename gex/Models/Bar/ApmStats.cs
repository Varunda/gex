using System.Collections.Generic;

namespace gex.Models.Bar {

    public class ApmStats {

        public string GameID { get; set; } = "";

        public byte PlayerID { get; set; }

        public int ActionCount { get; set; }

        public List<ApmPeriod> Periods { get; set; } = [];

    }

    public class ApmPeriod {

        public int ActionCount { get; set; }

        public double TimeStart { get; set; }

        public double TimeDuration { get; set; }

    }

}
