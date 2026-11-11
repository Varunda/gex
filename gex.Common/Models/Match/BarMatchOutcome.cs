using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Match {

    public class BarMatchOutcome {

        public int AllyTeamID { get; set; }

        public double Mu { get; set; }

        public double Sigma { get; set; }

        public double WinProbability { get; set; }

    }
}
