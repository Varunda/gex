using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class LobbyAlert {

        /// <summary>
        ///     unique ID of the alert
        /// </summary>
        [ColumnMapping("id")]
        public long ID { get; set; }

        /// <summary>
        ///     ID of the guild that has the channel the alert will be posted in
        /// </summary>
        [ColumnMapping("guild_id")]
        public ulong GuildID { get; set; }

        /// <summary>
        ///     ID of the channel to post the alert in
        /// </summary>
        [ColumnMapping("channel_id")]
        public ulong ChannelID { get; set; }

        /// <summary>
        ///     ID of the role to ping. null to not ping
        /// </summary>
        [ColumnMapping("role_id")]
        public ulong? RoleID { get; set; }

        /// <summary>
        ///     discord ID of the account that created this alert
        /// </summary>
        [ColumnMapping("created_by_id")]
        public ulong CreatedByID { get; set; }

        /// <summary>
        ///     minimum time between alerts in seconds
        /// </summary>
        [ColumnMapping("time_between_alerts_seconds")]
        public int TimeBetweenAlertsSeconds { get; set; }

        /// <summary>
        ///     when this alert was created
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     what map is playing
        /// </summary>
        [ColumnMapping("map")]
        public string? Map { get; set; }

        /// <summary>
        ///     minimum OS of all players
        /// </summary>
        [ColumnMapping("minimum_os")]
        public int? MinimumOS { get; set; }

        /// <summary>
        ///     maximum OS of all players
        /// </summary>
        [ColumnMapping("maximum_os")]
        public int? MaximumOS { get; set; }

        /// <summary>
        ///     minimum average OS over all players
        /// </summary>
        [ColumnMapping("minimum_average_os")]
        public int? MinimumAverageOS { get; set; }

        /// <summary>
        ///     maximum average OS over all players
        /// </summary>
        [ColumnMapping("maximum_average_os")]
        public int? MaximumAverageOS { get; set; }

        /// <summary>
        ///     minimum number of players
        /// </summary>
        [ColumnMapping("minimum_player_count")]
        public int? MinimumPlayerCount { get; set; }

        /// <summary>
        ///     maximum number of players
        /// </summary>
        [ColumnMapping("maximum_player_count")]
        public int? MaximumPlayerCount { get; set; }

        /// <summary>
        ///     gamemode of the lobby
        /// </summary>
        [ColumnMapping("gamemode")]
        public byte? Gamemode { get; set; }

    }
}
