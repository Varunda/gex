using gex.Code.Constants;

namespace gex.Models.Queues {

    public class UserMapStatUpdateQueueEntry {

        public long UserID { get; set; }

        public string Map { get; set; } = "";

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

    }
}
