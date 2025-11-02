using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
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
                    id, username, last_updated, country_code
                ) VALUES (
                    @ID, @Username, @LastUpdated, @CountryCode
                ) ON CONFLICT (id) DO UPDATE SET
                    username = @Username,
                    last_updated = @LastUpdated,
                    country_code = @CountryCode;
            ", cancel);

            cmd.AddParameter("ID", userID);
            cmd.AddParameter("Username", user.Username);
            cmd.AddParameter("LastUpdated", user.LastUpdated);
            cmd.AddParameter("CountryCode", user.CountryCode);
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
        ///     select all <see cref="BarUser"/>s from the DB
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarUser>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarUser>(
                "SELECT * FROM bar_user",
                cancel
            );
        }

        /// <summary>
        ///     search for user by name, and optionally previous names. case-insensitive
        /// </summary>
        /// <param name="name">name to search for</param>
        /// <param name="includePreviousNames">will previous names be searched as well</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a list of <see cref="UserSearchResult"/>s</returns>
        public async Task<List<UserSearchResult>> SearchByName(string name, bool includePreviousNames, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<UserSearchResult>(
                includePreviousNames == true
                    ? @"SELECT distinct(u.id) ""user_id"", u.username, u.last_updated, p.user_name ""previous_name""
                            FROM bar_user u LEFT JOIN bar_match_player p ON p.user_id = u.id
                            WHERE lower(u.username) LIKE lower(@Search) OR lower(p.user_name) LIKE lower(@Search)"
                    : @"SELECT id ""user_id"", username, last_updated, username ""previous_name"" FROM bar_user WHERE lower(username) LIKE lower(@Search)",
                new { Search = $"%{name}%" },
                cancellationToken: cancel
            );
        }


        /// <summary>
        ///     Get users by username matches, and optionally previous names. Case-insensitive.
        ///     Uses equality matching instead of LIKE pattern matching.
        /// </summary>
        /// <param name="usernames">List of usernames to find</param>
        /// <param name="includePreviousNames">If true, previous names will be searched as well</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>A list of <see cref="BarUser"/>s</returns>
        public async Task<List<BarUser>> GetByUsernames(List<string> usernames, bool includePreviousNames, CancellationToken cancel) {
            if (usernames == null || usernames.Count == 0) {
                return new List<BarUser>();
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            
            // Convert usernames to lowercase for case-insensitive matching
            string[] lowercaseUsernames = usernames.Select(n => n.ToLower()).ToArray();

            return await conn.QueryListAsync<BarUser>(
                includePreviousNames == true
                    ? @"SELECT DISTINCT u.id, u.username, u.last_updated, u.country_code
                            FROM bar_user u LEFT JOIN bar_match_player p ON p.user_id = u.id
                            WHERE lower(u.username) = ANY(@Usernames) OR lower(p.user_name) = ANY(@Usernames)"
                    : @"SELECT id, username, last_updated, country_code 
                        FROM bar_user 
                        WHERE lower(username) = ANY(@Usernames)",
                new { Usernames = lowercaseUsernames },
                cancellationToken: cancel
            );
        }

        /// <summary>
        ///     get all names that a user has used
        /// </summary>
        /// <param name="userID">ID of the user to get the previous names of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of <see cref="UserPreviousName"/>s that represent the past names of a user
        /// </returns>
        public async Task<List<UserPreviousName>> GetUserNames(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<UserPreviousName>(
                @"
                    SELECT user_name, min(m.start_time) ""timestamp""
                    FROM bar_match_player p LEFT JOIN bar_match m ON m.id = p.game_id
                    WHERE p.user_id = @UserID
                    GROUP BY p.user_name
                    ORDER BY 2 DESC;
                ",
                new { UserID = userID },
                cancel
            );
        }

    }
}
