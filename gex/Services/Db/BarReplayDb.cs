using Dapper;
using gex.Code.ExtensionMethods;
using gex.Common.Models.Bar;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BarReplayDb {

        private readonly ILogger<BarReplayDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarReplayDb(ILogger<BarReplayDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(BarReplay replay, CancellationToken cancel) {
            if (string.IsNullOrEmpty(replay.ID)) {
                throw new System.Exception($"missing ID from bar replay");
            }
            if (string.IsNullOrEmpty(replay.FileName)) {
                throw new System.Exception($"missing filename from bar replay");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                INSERT INTO bar_replay (
                    id, filename, map_name
                ) VALUES (
                    @ID, @FileName, @MapName
                );
            ", cancel);

            cmd.AddParameter("ID", replay.ID);
            cmd.AddParameter("FileName", replay.FileName);
            cmd.AddParameter("MapName", replay.MapName);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     get a <see cref="BarReplay"/> by <see cref="BarReplay.ID"/>
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
        public async Task<BarReplay?> GetByID(string gameID) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryFirstOrDefaultAsync<BarReplay>(
                "SELECT * FROM bar_replay WHERE id = @ID",
                new { ID = gameID }
            );
        }

    }
}
