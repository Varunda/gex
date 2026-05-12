using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
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
        ///     generate the user units made for the given map, gamemode and day
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Generate(UserUnitsMadeNeedsUpdate entry, CancellationToken cancel) {

            using NpgsqlConnection evConn = _DbHelper.Connection(Dbs.EVENT);
            List<BarUserUnitsMade> unitsMade = (await evConn.QueryAsync<BarUserUnitsMade>(
                new CommandDefinition(@"
                    SELECT
                        mp.user_id ""user_id"",
                        date_trunc('day', m.start_time) ""day"",
                        m.map_name ""map_filename"",
                        m.gamemode ""gamemode"", 
                        gum.definition_name ""definition_name"",
                        sum(gum.count) ""count"",
                        NOW() at time zone 'utc' ""timestamp""
                    from
                        bar_match_player mp
                        INNER JOIN bar_match m ON mp.game_id = m.id
                        LEFT JOIN game_units_created gum ON gum.game_id = mp.game_id AND mp.team_id = gum.team_id
                    WHERE
                        mp.user_id = @UserID
                        AND m.map_name = @MapFilename
                        AND m.gamemode = @Gamemode
                        AND date_trunc('day', m.start_time) = @Day
                        AND gum.definition_name <> ''
                    GROUP BY 1, 2, 3, 4, 5;
                ",
                new {
                    UserID = entry.UserID,
                    MapFilename = entry.MapFilename,
                    Gamemode = entry.Gamemode,
                    Day = entry.Day
                },
                commandTimeout: 120,
                cancellationToken: cancel)
            )).ToList();

            if (unitsMade.Count == 0) {
                _Logger.LogWarning($"failed to generate any unitsMade [userID={entry.UserID}] [day={entry.Day:u}] [map={entry.MapFilename}] [gamemode={entry.Gamemode}]");
                return;
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, $@"
				BEGIN TRANSACTION;

				DELETE FROM user_units_made
                WHERE 
                    user_id = @UserID
                    AND map_filename = @MapFileName 
                    AND gamemode = @Gamemode
                    AND day = @Day;

				INSERT INTO user_units_made (
					user_id, map_filename, gamemode, day,
                    definition_name, count,
					timestamp
				) VALUES 
                    {string.Join(",\n", unitsMade.Select((iter, index) =>
                        $@"(@UserID, @MapFileName, @Gamemode, @Day,
                            @DefName{index}, @Count{index},
                            NOW() at time zone 'utc'
                        )"
                    ))};

				COMMIT TRANSACTION;
			", cancel);

            //_Logger.LogDebug($"{cmd.Print()}");

            cmd.AddParameter("UserID", entry.UserID);
            cmd.AddParameter("MapFileName", entry.MapFilename);
            cmd.AddParameter("Gamemode", entry.Gamemode);
            cmd.AddParameter("Day", entry.Day);

            for (int i = 0; i < unitsMade.Count; ++i) {
                BarUserUnitsMade opener = unitsMade[i];
                cmd.AddParameter($"@DefName{i}", opener.DefinitionName);
                cmd.AddParameter($"@Count{i}", opener.Count);
            }

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();

            return;
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
