using gex.Common.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Coven.Models.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match {

    public class BarMatchHashDb {

        private readonly ILogger<BarMatchHashDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchHashDb(ILogger<BarMatchHashDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarMatchHash>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QueryListAsync<BarMatchHash>(
                "SELECT * FROM bar_match_hash;",
                cancel
            );
        }

        public async Task<BarMatchHash?> GetByID(string gameID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QuerySingleAsync<BarMatchHash>(
                $"SELECT * FROM bar_match_hash WHERE id = @GameID;",
                new {
                    GameID = gameID
                },
                cancel
            );
        }

        public async Task<BarMatchHash?> GetByHash(string hash, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QuerySingleAsync<BarMatchHash>(
                $"SELECT * FROM bar_match_hash WHERE hash = @Hash;",
                new {
                    Hash = hash.ToLower()
                },
                cancel
            );
        }

        public async Task Upsert(BarMatchHash hash, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_hash (
                    id, filename, hash
                ) VALUES (
                    @GameID, @FileName, @Hash
                ) ON CONFLICT (id) DO
                    UPDATE SET hash = @Hash;
            ", cancel);

            cmd.AddParameter("GameID", hash.GameID);
            cmd.AddParameter("FileName", hash.FileName);
            cmd.AddParameter("Hash", hash.Hash.ToLower());
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
        }

    }
}
