using System;

namespace gex.Models.Db {

    public class BarMatchProcessingPriority {

        public ulong DiscordID { get; set; }

        public string GameID { get; set; } = "";

        public DateTime Timestamp { get; set; }

    }
}
