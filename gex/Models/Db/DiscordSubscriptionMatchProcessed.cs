using Dapper.ColumnMapper;
using gex.Code;
using gex.Models.UserStats;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class DiscordSubscriptionMatchProcessed {

        /// <summary>
        ///     unique ID of the entry
        /// </summary>
        [ColumnMapping("id")]
        public long ID { get; set; }

        /// <summary>
        ///     ID of the <see cref="BarUser"/> that will send a message when a match is processed
        ///     that they are in
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        /// <summary>
        ///     ID of the Discord account that will get the message when a processed match contains <see cref="UserID"/>
        /// </summary>
        [ColumnMapping("discord_id")]
        public ulong DiscordID { get; set; }

        /// <summary>
        ///     timestamp of when this entry was created
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
