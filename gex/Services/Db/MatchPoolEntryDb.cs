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

        /// <summary>
        ///     get a list of all <see cref="MatchPoolEntry"/>s a match is a part of
        /// </summary>
        /// <param name="matchID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<MatchPoolEntry>> GetByMatchID(string matchID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MatchPoolEntry>(
                "SELECT * FROM match_pool_entry WHERE match_id = @MatchID",
                new { MatchID = matchID },
                cancel
            );
        }

        public async Task<MatchPoolEntry?> GetByPoolAndMatchID(long poolID, string matchID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<MatchPoolEntry>(
                "SELECT * FROM match_pool_entry WHERE pool_id = @PoolID AND match_id = @MatchID",
                new { PoolID = poolID, MatchID = matchID },
                cancel
            );
        }

        /// <summary>
        ///     insert a new <see cref="MatchPoolEntry"/> into the DB
        /// </summary>
        /// <param name="entry">entry to insert</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task Insert(MatchPoolEntry entry, CancellationToken cancel) {
            if (string.IsNullOrEmpty(entry.MatchID)) {
                throw new Exception($"missing {nameof(MatchPoolEntry.MatchID)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO match_pool_entry (
                    pool_id, match_id, added_by_id, description, timestamp
                ) VALUES (
                    @PoolID, @MatchID, @AddedByID, @Description, NOW() at time zone 'utc'
                );
            ", cancel);

            cmd.AddParameter("PoolID", entry.PoolID);
            cmd.AddParameter("MatchID", entry.MatchID);
            cmd.AddParameter("AddedByID", entry.AddedByID);
            cmd.AddParameter("Description", entry.Description);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     update the <see cref="MatchPoolEntry.Description"/> of an entry
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task UpdateDescription(MatchPoolEntry entry, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE match_pool_entry
                    SET description = @Description
                WHERE
                    pool_id = @PoolID
                    AND match_id = @MatchID;
            ");

            cmd.AddParameter("Description", entry.Description);
            cmd.AddParameter("PoolID", entry.PoolID);
            cmd.AddParameter("MatchID", entry.MatchID);
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
