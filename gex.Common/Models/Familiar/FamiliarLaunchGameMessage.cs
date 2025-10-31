using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Familiar {

    public class FamiliarLaunchGameMessage {

        public int BattleID { get; set; }

        public string HostIP { get; set; } = "";

        public int Port { get; set; }

        public int Secret { get; set; }

        public string Engine { get; set; } = "";

        public string GameVersion { get; set; } = "";

        public string Map { get; set; } = "";

    }

}
