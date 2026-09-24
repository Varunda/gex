using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Match {

    public class BarMatchEntity {

        public string Name { get; set; } = "";

        public IEnumerable<int> TeamIDs { get; set; } = [];

    }
}
