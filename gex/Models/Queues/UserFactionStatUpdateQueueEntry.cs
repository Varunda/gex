using gex.Code.Constants;

namespace gex.Models.Queues {

    public class UserFactionStatUpdateQueueEntry {

        public long UserID { get; set; }

        public byte Faction { get; set; } = BarFaction.DEFAULT;

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

    }
}
