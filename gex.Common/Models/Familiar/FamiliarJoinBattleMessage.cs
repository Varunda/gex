using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Familiar {

    public class FamiliarJoinBattleMessage {

        public int BattleID { get; set; }

        public string? Password { get; set; }

        public string InvitedBy { get; set; } = "";

        public int Secret { get; set; }

    }
}
