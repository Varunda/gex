using gex.Code.ExtensionMethods;
using gex.Models.Bar;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

    public class MapStatsStartSpotDb {

        private readonly ILogger<MapStatsStartSpotDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapStatsStartSpotDb(ILogger<MapStatsStartSpotDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MapStatsStartSpot>> GetByMap(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsStartSpot>(
                "SELECT * FROM map_stats_start_spot WHERE map_file_name = @MapFilename",
                new { MapFilename = mapFilename },
                cancel
            );
        }

        public async Task Generate(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
				BEGIN TRANSACTION;

				DELETE FROM map_stats_start_spot WHERE map_file_name = @MapFileName;
				
				INSERT INTO map_stats_start_spot (
					map_file_name, gamemode, timestamp,
					start_x, start_z, count_total, count_win
				) SELECT 
						m.map_name,
						m.gamemode,
						NOW() at time zone 'utc',
						128 * TRUNC(p.starting_position_x / 128),
						128 * TRUNC(p.starting_position_z / 128),
						count(*),
						count(*) filter (where at.won = true) 
					FROM 
						bar_match m
						LEFT JOIN bar_match_player p ON p.game_id = m.id
						LEFT JOIN bar_match_ally_team at ON p.ally_team_id = at.ally_team_id AND p.game_id = at.game_id
					WHERE 
						m.map_name = @MapFileName
						AND m.gamemode <> 0
					GROUP BY
						1, 2, 4, 5
					HAVING 
						COUNT(*) > 4;

				COMMIT TRANSACTION;
			", cancel);

            cmd.CommandTimeout = 60 * 5; // 5 minutes

            cmd.AddParameter("MapFileName", mapFilename);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     get start spot by map and user
        /// </summary>
        /// <param name="mapFilename">filename of the map (see <see cref="BarMap.FileName"/>)</param>
        /// <param name="userID">ID of the user to get the start spots of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<MapStatsStartSpot>> GetByMapAndUser(string mapFilename, long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsStartSpot>(@"
                    SELECT 
						m.map_name ""map_file_name"",
						m.gamemode ""gamemode"",
						NOW() at time zone 'utc' ""timestamp"",
						128 * TRUNC(p.starting_position_x / 128) ""start_x"",
						128 * TRUNC(p.starting_position_z / 128) ""start_z"",
						count(*) ""count_total"",
						count(*) filter (where at.won = true) ""count_win""
					FROM 
						bar_match m
						LEFT JOIN bar_match_player p ON p.game_id = m.id
						LEFT JOIN bar_match_ally_team at ON p.ally_team_id = at.ally_team_id AND p.game_id = at.game_id
					WHERE 
						m.map_name = @MapFileName
						AND m.gamemode <> 0
                        AND p.user_id = @UserID
					GROUP BY
						1, 2, 4, 5",
                new { MapFileName = mapFilename, UserID = userID },
                cancel
            );
        }

    }
}
