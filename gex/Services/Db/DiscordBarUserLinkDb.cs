using gex.Code.ExtensionMethods;
using gex.Models.Discord;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class DiscordBarUserLinkDb {

        private readonly ILogger<DiscordBarUserLinkDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public DiscordBarUserLinkDb(ILogger<DiscordBarUserLinkDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<DiscordBarUserLink?> GetByDiscordID(ulong discordID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<DiscordBarUserLink>(
                @"SELECT * FROM discord_bar_user_link WHERE discord_id = @DiscordID",
                new { DiscordID = unchecked((long)discordID) },
                cancel
            );
        }

        public async Task Upsert(DiscordBarUserLink link, CancellationToken cancel) {
            if (link.DiscordID == 0) {
                throw new ArgumentException($"missing {nameof(DiscordBarUserLink.DiscordID)} from {nameof(DiscordBarUserLink)}");
            }
            if (link.BarUserID == 0) {
                throw new ArgumentException($"missing {nameof(DiscordBarUserLink.BarUserID)} from {nameof(DiscordBarUserLink)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO discord_bar_user_link (
                    discord_id, bar_user_id, timestamp
                ) VALUES (
                    @DiscordID, @BarUserID, @Timestamp
                ) ON CONFLICT (discord_id) DO UPDATE
                    SET bar_user_id = @BarUserID,
                        timestamp = @Timestamp;
            ", cancel);

            cmd.AddParameter("DiscordID", link.DiscordID);
            cmd.AddParameter("BarUserID", link.BarUserID);
            cmd.AddParameter("Timestamp", DateTime.UtcNow);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task Unlink(ulong discordID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM discord_bar_user_link
                    WHERE discord_id = @DiscordID;
            ", cancel);

            cmd.AddParameter("DiscordID", discordID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
