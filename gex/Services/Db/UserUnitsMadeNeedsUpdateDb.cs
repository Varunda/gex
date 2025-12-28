using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    /// <summary>
    ///     defunct
    /// </summary>
    [Obsolete("only kept for when map opening labs update is done")]
    public class UserUnitsMadeNeedsUpdateDb {

        private readonly ILogger<UserUnitsMadeNeedsUpdateDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public UserUnitsMadeNeedsUpdateDb(ILogger<UserUnitsMadeNeedsUpdateDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get all <see cref="UserUnitsMadeNeedsUpdate"/> that are pending generation
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<UserUnitsMadeNeedsUpdate>> GetReady(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<UserUnitsMadeNeedsUpdate>(@"
                SELECT *
                FROM user_units_made_needs_update
                WHERE last_dirtied <= (NOW() at time zone 'utc' - '2 hours'::interval);
            ", cancel);
        }

        /// <summary>
        ///     get the <see cref="UserUnitsMadeNeedsUpdate"/> of a specific user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<UserUnitsMadeNeedsUpdate>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<UserUnitsMadeNeedsUpdate>(@"
                SELECT *
                FROM user_units_made_needs_update
                WHERE user_id = @UserID;
            ", new { UserID = userID }, cancel);
        }

        /// <summary>
        ///     upsert a <see cref="UserUnitsMadeNeedsUpdate"/>
        /// </summary>
        /// <param name="update"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Upsert(UserUnitsMadeNeedsUpdate update, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(update.MapFilename)) {
                throw new Exception($"missing {nameof(UserUnitsMadeNeedsUpdate.MapFilename)}");
            }
            if (update.Day == default) {
                throw new Exception($"missing {nameof(UserUnitsMadeNeedsUpdate.Day)}]");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO user_units_made_needs_update (
                    user_id, day, map_filename, last_dirtied
                ) VALUES (
                    @UserID, @Day, @MapFilename, @LastDirtied
                ) ON CONFLICT (user_id, day, map_filename) DO UPDATE
                    SET last_dirtied = @LastDirtied;
            ", cancel);

            cmd.AddParameter("UserID", update.UserID);
            cmd.AddParameter("Day", update.Day.Date);
            cmd.AddParameter("MapFilename", update.MapFilename);
            cmd.AddParameter("LastDirtied", update.LastDirtied);

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     remove a <see cref="UserUnitsMadeNeedsUpdate"/> from the db, marking it as completed
        /// </summary>
        /// <param name="update"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Remove(UserUnitsMadeNeedsUpdate update, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM user_units_made_needs_update
                    WHERE user_id = @UserID
                        AND day = @Day
                        AND map_filename = @MapFilename
            ", cancel);

            cmd.AddParameter("UserID", update.UserID);
            cmd.AddParameter("Day", update.Day);
            cmd.AddParameter("MapFilename", update.MapFilename);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
