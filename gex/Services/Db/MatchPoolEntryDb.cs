using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class MatchPoolEntryDb {

        private readonly ILogger<MatchPoolEntryDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MatchPoolEntryDb(ILogger<MatchPoolEntryDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MatchPoolEntry>> GetByPoolID(long poolID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MatchPoolEntry>(
                "SELECT * FROM match_pool_entry WHERE pool_id = @PoolID",
                new { PoolID = poolID },
                cancel
            );
        }

        public async Task Insert(MatchPoolEntry entry, CancellationToken cancel) {
            if (string.IsNullOrEmpty(entry.MatchID)) {
                throw new Exception($"missing {nameof(MatchPoolEntry.MatchID)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO match_pool_entry (
                    pool_id, match_id, added_by_id, timestamp
                ) VALUES (
                    @PoolID, @MatchID, @AddedByID, NOW() at time zone 'utc'
                );
            ", cancel);

            cmd.AddParameter("PoolID", entry.PoolID);
            cmd.AddParameter("MatchID", entry.MatchID);
            cmd.AddParameter("AddedByID", entry.AddedByID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task Remove(MatchPoolEntry entry, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM match_pool_entry
                    WHERE pool_id = @PoolID
                        AND match_id = @MatchID;
            ", cancel);

            cmd.AddParameter("PoolID", entry.PoolID);
            cmd.AddParameter("MatchID", entry.MatchID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
