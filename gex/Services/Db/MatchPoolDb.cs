using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class MatchPoolDb {

        private readonly ILogger<MatchPoolDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MatchPoolDb(ILogger<MatchPoolDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MatchPool>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MatchPool>(
                "SELECT * FROM match_pool",
                cancel
            );
        }

        public async Task<MatchPool?> GetByID(long ID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<MatchPool>(
                "SELECT * FROM match_pool WHERE id = @ID",
                new { ID = ID },
                cancel
            );
        }

        public async Task<long> Create(MatchPool pool, CancellationToken cancel) {
            if (string.IsNullOrEmpty(pool.Name)) {
                throw new Exception($"missing {nameof(MatchPool.Name)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO match_pool (
                    name, created_by_id, timestamp
                ) VALUES (
                    @Name, @CreatedByID, NOW() at time zone 'utc'
                ) RETURNING id;
            ", cancel);

            cmd.AddParameter("Name", pool.Name);
            cmd.AddParameter("CreatedByID", pool.CreatedByID);
            await cmd.PrepareAsync(cancel);

            long ID = await cmd.ExecuteInt64(cancel);
            await conn.CloseAsync();
            return ID;
        }

        public async Task UpdateName(long poolID, MatchPool pool, CancellationToken cancel) {
            if (string.IsNullOrEmpty(pool.Name)) {
                throw new Exception($"missing {nameof(MatchPool.Name)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE match_pool
                    SET name = @Name
                    WHERE id = @ID;
            ", cancel);

            cmd.AddParameter("Name", pool.Name);
            cmd.AddParameter("ID", poolID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
