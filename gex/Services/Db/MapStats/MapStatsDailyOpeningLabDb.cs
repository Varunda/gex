using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

    public class MapStatsDailyOpeningLabDb {

        private readonly ILogger<MapStatsDailyOpeningLabDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapStatsDailyOpeningLabDb(ILogger<MapStatsDailyOpeningLabDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MapStatsDailyOpeningLab>> GetByMap(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsDailyOpeningLab>(
                "SELECT * FROM map_stats_daily_opening_lab WHERE map_file_name = @MapFilename",
                new { MapFilename = mapFilename },
                cancel
            );
        }

        public async Task Generate(MapStatsNeedsUpdate entry, CancellationToken cancel) {
            using NpgsqlConnection evConn = _DbHelper.Connection(Dbs.EVENT);

            List<MapStatsDailyOpeningLab> openers = (await evConn.QueryAsync<MapStatsDailyOpeningLab>(new CommandDefinition(
                @"
                    WITH first_lab AS (
                        SELECT
                            bm.id,
                            geucd.team_id,
                            json_agg(geucd.definition_name order by frame)->>0 ""defname""
                        FROM
                            bar_match bm
                            left join game_event_unit_created geucd on bm.id = geucd.game_id
                        WHERE
                            date_trunc('day', bm.start_time) = @Day
                            AND bm.gamemode = @Gamemode
                            AND bm.map_name = @MapFilename
                            AND geucd.definition_name in (
                                'armlab', 'armvp', 'armap', 'armsy', 'armhp', 'armfhp',
                                'corlab', 'corvp', 'corap', 'corsy', 'corhp', 'corfhp',
                                'leglab', 'legvp', 'legap', 'legsy', 'leghp', 'legfhp'
                            )
                        GROUP BY
                            bm.id, geucd.team_id
                    )
                    SELECT
                        m.map_name ""map_filename"",
                        m.gamemode ""gamemode"",
                        fl.defname ""definition_name"",
                        date_trunc('day', m.start_time) ""day"",

                        NOW() at time zone 'utc' ""timestamp"",

                        count(*) ""count"",
                        count(*) filter (where at.won = true) ""wins""
                    FROM
                        first_lab fl
                        left join bar_match m ON fl.id = m.id
                        left join bar_match_player p ON fl.id = p.game_id AND fl.team_id = p.team_id
                        left join bar_match_ally_team at ON fl.id = at.game_id AND p.ally_team_id = at.ally_team_id
                    WHERE m.gamemode <> 0
                    GROUP BY 1, 2, 3, 4;
                ",
                new { 
                    MapFileName = entry.MapFilename,
                    Day = entry.Day,
                    Gamemode = entry.Gamemode
                },
                commandTimeout: 60 * 5,
                cancellationToken: cancel
            ))).ToList();

            if (openers.Count == 0) {
                _Logger.LogDebug($"map has no opening labs for this entry [map={entry.MapFilename}] [gamemode={entry.Gamemode}] [day={entry.Day:u}]");
                return;
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, $@"
				BEGIN TRANSACTION;

				DELETE FROM map_stats_daily_opening_lab 
                WHERE 
                    map_filename = @MapFileName 
                    AND gamemode = @Gamemode
                    AND day = @Day;

				INSERT INTO map_stats_daily_opening_lab (
					map_filename, gamemode, day,
                    definition_name, count, wins,
					timestamp
				) VALUES 
                    {string.Join(",\n", openers.Select((iter, index) =>
                        $@"(@MapFileName, @Gamemode, @Day,
                            @DefName{index}, @Count{index}, @Wins{index},
                            NOW() at time zone 'utc'
                        )"
                    ))};

				COMMIT TRANSACTION;
			", cancel);

            //_Logger.LogDebug($"{cmd.Print()}");

            cmd.AddParameter("MapFileName", entry.MapFilename);
            cmd.AddParameter("Gamemode", entry.Gamemode);
            cmd.AddParameter("Day", entry.Day);

            for (int i = 0; i < openers.Count; ++i) {
                MapStatsDailyOpeningLab opener = openers[i];
                cmd.AddParameter($"@DefName{i}", opener.DefinitionName);
                cmd.AddParameter($"@Count{i}", opener.Count);
                cmd.AddParameter($"@Wins{i}", opener.Wins);
            }

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
