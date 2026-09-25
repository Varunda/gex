using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Match {

    public class BarMatchMilestone {

        public BarMatchEntity Entity { get; set; } = new();

        public long Frame { get; set; }

        public int Interest { get; set; }

        public string Action { get; set; } = "";

        public string? UnitIcon { get; set; }

    }
}
