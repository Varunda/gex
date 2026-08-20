using gex.Code.ExtensionMethods;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class MapStatsPositionLeaderboardDb {

        private readonly ILogger<MapStatsPositionLeaderboardDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapStatsPositionLeaderboardDb(ILogger<MapStatsPositionLeaderboardDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MapPositionLeaderboardEntry>> GetByMapFilename(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapPositionLeaderboardEntry>(
                @"SELECT * FROM map_position_leaderboard_entry WHERE map_filename = @MapFilename",
                new {
                    MapFilename = mapFilename
                },
                cancel
            );
        }

        public async Task Generate(string mapFilename, string positionLabel, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                BEGIN TRANSACTION;

                DELETE FROM map_position_leaderboard_entry
                    WHERE map_filename = @MapFilename
                        AND position_label = @PositionLabel;

                INSERT INTO map_position_leaderboard_entry (
                    user_id, map_filename, position_label,
                    score, play_count, win_count, average_enemy_skill,
                    timestamp
                ) SELECT
                    mp.user_id,
                    m.map_name,
                    t.start_spot_label,

                    (
                        count(*) filter (WHERE at.won = true)
                        / count(*)::numeric
                        * avg(enemy_at.average_skill)::numeric
                    ) ""score"",
                    count(*) ""play_count"",
                    count(*) filter (WHERE at.won = true) ""win_count"",
                    avg(enemy_at.average_skill) ""average_enemy_os"",
        
                    NOW() at time zone 'utc'
                FROM
                    bar_match_player mp
                    INNER JOIN bar_match_team t ON t.game_id = mp.game_id
                    INNER JOIN bar_match m ON mp.game_id = m.id
                    INNER JOIN bar_match_ally_team at ON at.game_id = mp.game_id AND mp.ally_team_id = at.ally_team_id
                    INNER JOIN bar_match_ally_team enemy_at ON enemy_at.game_id = m.id AND mp.ally_team_id <> enemy_at.ally_team_id
                WHERE
                    m.gamemode = 3
                    AND m.map_name = @MapFilename
                    AND t.start_spot_label = @PositionLabel
                GROUP BY
                    mp.user_id, m.map_name, t.start_spot_label
                HAVING
                    count(*) > 50
                ORDER BY 4 desc
                LIMIT 200;

                COMMIT TRANSACTION;
            ");

            cmd.AddParameter("MapFilename", mapFilename);
            cmd.AddParameter("PositionLabel", positionLabel);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }

}
