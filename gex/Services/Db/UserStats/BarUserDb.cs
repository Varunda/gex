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

    public class BarUserDb {

        private readonly ILogger<BarUserDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarUserDb(ILogger<BarUserDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     update/insert (upsert) a <see cref="BarUser"/>
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="user"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Upsert(long userID, BarUser user, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_user (
                    id, username, last_updated
                ) VALUES (
                    @ID, @Username, @LastUpdated
                ) ON CONFLICT (id) DO UPDATE SET
                    username = @Username,
                    last_updated = @LastUpdated;
            ", cancel);

            cmd.AddParameter("ID", userID);
            cmd.AddParameter("Username", user.Username);
            cmd.AddParameter("LastUpdated", user.LastUpdated);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     get a specific <see cref="BarUser"/> by <see cref="BarUser.UserID"/>
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<BarUser?> GetByID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryFirstOrDefaultAsync<BarUser>(new CommandDefinition(
                "SELECT * FROM bar_user WHERE id = @UserID",
                new { UserID = userID },
                cancellationToken: cancel
            ));
        }

        /// <summary>
        ///     search for user by name
        /// </summary>
        /// <param name="name">name to search for</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarUser>> SearchByName(string name, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<BarUser>(new CommandDefinition(
                "SELECT * FROM bar_user WHERE lower(username) LIKE lower(@Search)",
                new { Search = $"%{name}%" },
                cancellationToken: cancel
            ))).ToList();
        }

    }
}
