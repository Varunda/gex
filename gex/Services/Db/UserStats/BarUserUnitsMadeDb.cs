using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.UserStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.UserStats {

    public class BarUserUnitsMadeDb {

        private readonly ILogger<BarUserUnitsMadeDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarUserUnitsMadeDb(ILogger<BarUserUnitsMadeDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     generate <see cref="BarUserUnitsMade"/> results for a set of users, on a specific day,
        ///     on specific maps
        /// </summary>
        /// <param name="userIDs"></param>
        /// <param name="day"></param>
        /// <param name="mapFilenames"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<BarUserUnitsMade>> Generate(IEnumerable<long> userIDs, DateTime day,
            IEnumerable<string> mapFilenames, CancellationToken cancel) {

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            return (await conn.QueryAsync<BarUserUnitsMade>(
                new CommandDefinition(@"
                    SELECT
                        mp.user_id ""user_id"",
                        date_trunc('day', m.start_time) ""day"",
                        m.map_name ""map_filename"",
                        c.definition_name ""definition_name"",
                        count(*) ""count"",
                        NOW() at time zone 'utc' ""timestamp""
                    from
                        bar_match_player mp
                        INNER JOIN bar_match m ON mp.game_id = m.id
                        LEFT JOIN game_event_unit_created c ON mp.game_id = c.game_id AND mp.team_id = c.team_id
                    WHERE
                        mp.user_id = ANY(@UserIDs)
                        AND m.map_name = ANY(@MapFilenames)
                        AND date_trunc('day', m.start_time) = @Day
                        AND c.definition_name <> ''
                    GROUP BY 1, 2, 3, 4;
                ",
                new { 
                    UserIDs = userIDs.ToList(),
                    MapFilenames = mapFilenames.ToList(),
                    Day = day
                },
                commandTimeout: 120,
                cancellationToken: cancel))
            ).ToList();
        }

        /// <summary>
        ///     get the <see cref="BarUserUnitsMade"/>s for a specific user
        /// </summary>
        /// <param name="userID">ID of the user to get the unit create count for</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarUserUnitsMade>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarUserUnitsMade>(@"
                    SELECT *
                    FROM user_units_made
                    WHERE user_id = @UserID;
                ",
                new { UserID = userID },
                cancel
            );
        }

        public async Task Upsert(BarUserUnitsMade made, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO user_units_made (
                    user_id, day, map_filename, definition_name, count, timestamp
                ) VALUES (
                    @UserID, @Day, @MapFilename, @DefinitionName, @Count, @Timestamp
                ) ON CONFLICT (user_id, day, map_filename, definition_name)
                    DO UPDATE SET count = @Count,
                        timestamp = @Timestamp;
            ", cancel);

            cmd.AddParameter("UserID", made.UserID);
            cmd.AddParameter("Day", made.Day);
            cmd.AddParameter("MapFilename", made.MapFilename);
            cmd.AddParameter("DefinitionName", made.DefinitionName);
            cmd.AddParameter("Count", made.Count);
            cmd.AddParameter("Timestamp", made.Timestamp);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
