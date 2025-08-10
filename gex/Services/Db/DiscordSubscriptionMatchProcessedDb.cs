using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class DiscordSubscriptionMatchProcessedDb {

        private readonly ILogger<DiscordSubscriptionMatchProcessedDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public DiscordSubscriptionMatchProcessedDb(ILogger<DiscordSubscriptionMatchProcessedDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     insert a new <see cref="DiscordSubscriptionMatchProcessed"/>
        /// </summary>
        /// <param name="sub">parameters to insert the new subscription with</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>the <see cref="DiscordSubscriptionMatchProcessed.ID"/> of the newly insert entry</returns>
        /// <exception cref="ArgumentException">
        ///     if <paramref name="sub"/> has a <see cref="DiscordSubscriptionMatchProcessed.UserID"/>
        ///     or <see cref="DiscordSubscriptionMatchProcessed.DiscordID"/> of <code>0</code>
        /// </exception>
        public async Task<ulong> Insert(DiscordSubscriptionMatchProcessed sub, CancellationToken cancel) {
            if (sub.UserID == 0) {
                throw new ArgumentException($"missing {nameof(DiscordSubscriptionMatchProcessed.UserID)} of {nameof(DiscordSubscriptionMatchProcessed)}");
            }
            if (sub.DiscordID == 0) {
                throw new ArgumentException($"missing {nameof(DiscordSubscriptionMatchProcessed.DiscordID)} of {nameof(DiscordSubscriptionMatchProcessed)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO discord_subscription_match_processed (
                    user_id, discord_id, timestamp
                ) VALUES (
                    @UserID, @DiscordID, NOW() at time zone 'utc'
                ) RETURNING id;
            ", cancel);

            cmd.AddParameter("UserID", sub.UserID);
            cmd.AddParameter("DiscordID", sub.DiscordID);
            await cmd.PrepareAsync(cancel);

            ulong id = await cmd.ExecuteUInt64(cancel);
            await conn.CloseAsync();

            return id;
        }

        /// <summary>
        ///     get the <see cref="DiscordSubscriptionMatchProcessed"/> for a user ID
        /// </summary>
        /// <param name="userID">ID of the user account to get the <see cref="DiscordSubscriptionMatchProcessed"/> of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of <see cref="DiscordSubscriptionMatchProcessed"/> with a 
        ///     <see cref="DiscordSubscriptionMatchProcessed.UserID"/> of <paramref name="userID"/>
        /// </returns>
        public async Task<List<DiscordSubscriptionMatchProcessed>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<DiscordSubscriptionMatchProcessed>(
                "SELECT * FROM discord_subscription_match_processed WHERE user_id = @UserID",
                new { UserID = userID },
                cancel
            );
        }

        /// <summary>
        ///     get the <see cref="DiscordSubscriptionMatchProcessed"/> for a discord ID
        /// </summary>
        /// <param name="discordID">ID of the discord account to get the <see cref="DiscordSubscriptionMatchProcessed"/> of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of <see cref="DiscordSubscriptionMatchProcessed"/> with a 
        ///     <see cref="DiscordSubscriptionMatchProcessed.DiscordID"/> of <paramref name="discordID"/>
        /// </returns>
        public async Task<List<DiscordSubscriptionMatchProcessed>> GetByDiscordID(ulong discordID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<DiscordSubscriptionMatchProcessed>(
                "SELECT * FROM discord_subscription_match_processed WHERE discord_id = @DiscordID",
                new { DiscordID = unchecked((long)(ulong)discordID) },
                cancel
            );
        }

        /// <summary>
        ///     remove a <see cref="DiscordSubscriptionMatchProcessed"/> by its
        ///     <see cref="DiscordSubscriptionMatchProcessed.ID"/>
        /// </summary>
        /// <param name="ID">ID of the <see cref="DiscordSubscriptionMatchProcessed"/> to remove</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for when the async operation is complete</returns>
        public async Task Remove(long ID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM discord_subscription_match_processed
                    WHERE id = @ID;
            ", cancel);

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
