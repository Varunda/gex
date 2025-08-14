using System;

namespace gex.Models.Db {

    public class LobbyAlert {

        public long ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong ChannelID { get; set; }

        public ulong RoleID { get; set; }

        public ulong CreatedByID { get; set; }

        public int TimeBetweenAlertsSeconds { get; set; }

        public DateTime Timestamp { get; set; }

        public int? MinimumOS { get; set; }

        public int? MaximumOS { get; set; }

        public int? MinimumAverageOS { get; set; }

        public int? MaximumAverageOS { get; set; }

        public int? MinimumPlayerCount { get; set; }

        public int? MaximumPlayerCount { get; set; }



    }
}
