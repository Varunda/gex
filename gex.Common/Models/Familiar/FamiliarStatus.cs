using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Familiar {

    public class FamiliarStatus {

        public string Name { get; set; } = "";

        public bool InBattle { get; set; } = false;

        public int? BattleID { get; set; } = null;

        public int? Secret { get; set; } = null;

        public bool HasLua { get; set; } = false;

    }
}
