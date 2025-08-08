using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Discord {

    /// <summary>
    ///     links a discord user to a BAR user
    /// </summary>
    [DapperColumnsMapped]
    public class DiscordBarUserLink {

        /// <summary>
        ///     ID of the Discord account
        /// </summary>
        [ColumnMapping("discord_id")]
        public ulong DiscordID { get; set; }

        /// <summary>
        ///     ID of the BAR user
        /// </summary>
        [ColumnMapping("bar_user_id")]
        public long BarUserID { get; set; }

        /// <summary>
        ///     timestamp of when this entry was created
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
