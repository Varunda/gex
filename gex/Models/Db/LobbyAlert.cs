using System;

namespace gex.Models.Db {

    public class LobbyAlert {

        /// <summary>
        ///     unique ID of the alert
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        ///     ID of the guild that has the channel the alert will be posted in
        /// </summary>
        public ulong GuildID { get; set; }

        /// <summary>
        ///     ID of the channel to post the alert in
        /// </summary>
        public ulong ChannelID { get; set; }

        /// <summary>
        ///     ID of the role to ping
        /// </summary>
        public ulong RoleID { get; set; }

        /// <summary>
        ///     discord ID of the account that created this alert
        /// </summary>
        public ulong CreatedByID { get; set; }

        /// <summary>
        ///     minimum time between alerts in seconds
        /// </summary>
        public int TimeBetweenAlertsSeconds { get; set; }

        /// <summary>
        ///     when this alert was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     minimum OS of all players
        /// </summary>
        public int? MinimumOS { get; set; }

        /// <summary>
        ///     maximum OS of all players
        /// </summary>
        public int? MaximumOS { get; set; }

        /// <summary>
        ///     minimum average OS over all players
        /// </summary>
        public int? MinimumAverageOS { get; set; }

        /// <summary>
        ///     maximum average OS over all players
        /// </summary>
        public int? MaximumAverageOS { get; set; }

        /// <summary>
        ///     minimum number of players
        /// </summary>
        public int? MinimumPlayerCount { get; set; }

        /// <summary>
        ///     maximum number of players
        /// </summary>
        public int? MaximumPlayerCount { get; set; }

    }
}
