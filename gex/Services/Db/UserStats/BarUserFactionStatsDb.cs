using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.UserStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.UserStats {

    public class BarUserFactionStatsDb {

        private readonly ILogger<BarUserFactionStatsDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarUserFactionStatsDb(ILogger<BarUserFactionStatsDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Upsert(BarUserFactionStats stats, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_user_faction_stats (
                    user_id, faction, gamemode,
                    play_count, win_count, loss_count, tie_count, last_updated
                ) VALUES (
                    @UserID, @Faction, @Gamemode,
                    @PlayCount, @WinCount, @LossCount, @TieCount, @LastUpdated
                ) ON CONFLICT (user_id, faction, gamemode) DO UPDATE SET
                    play_count = @PlayCount,
                    win_count = @WinCount,
                    loss_count = @LossCount,
                    tie_count = @TieCount,
                    last_updated = @LastUpdated
            ", cancel);

            cmd.AddParameter("UserID", stats.UserID);
            cmd.AddParameter("Faction", stats.Faction);
            cmd.AddParameter("Gamemode", stats.Gamemode);
            cmd.AddParameter("PlayCount", stats.PlayCount);
            cmd.AddParameter("WinCount", stats.WinCount);
            cmd.AddParameter("LossCount", stats.LossCount);
            cmd.AddParameter("TieCount", stats.TieCount);
            cmd.AddParameter("LastUpdated", stats.LastUpdated);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task<List<BarUserFactionStats>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<BarUserFactionStats>(new CommandDefinition(
                "SELECT * FROM bar_user_faction_stats WHERE user_id = @UserID",
                new { UserID = userID },
                cancellationToken: cancel
            ))).ToList();
        }


    }
}
