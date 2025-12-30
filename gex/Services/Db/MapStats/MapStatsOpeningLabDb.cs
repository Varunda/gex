using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

    public class MapStatsOpeningLabDb {

        private readonly ILogger<MapStatsOpeningLabDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapStatsOpeningLabDb(ILogger<MapStatsOpeningLabDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MapStatsOpeningLab>> GetByMap(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsOpeningLab>(
                "SELECT * FROM map_stats_opening_lab WHERE map_file_name = @MapFilename",
                new { MapFilename = mapFilename },
                cancel
            );
        }

        public async Task Generate(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection evConn = _DbHelper.Connection(Dbs.EVENT);

            List<MapStatsOpeningLab> openers = (await evConn.QueryAsync<MapStatsOpeningLab>(new CommandDefinition(
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
                            map_name = @MapFileName
                            and geucd.definition_name in (
                                'armlab', 'armvp', 'armap', 'armsy', 'armhp', 'armfhp',
                                'corlab', 'corvp', 'corap', 'corsy', 'corhp', 'corfhp',
                                'leglab', 'legvp', 'legap', 'legsy', 'leghp', 'legfhp'
                            )
                        GROUP BY
                            bm.id, geucd.team_id
                    )
                    SELECT
                        m.map_name,
                        m.gamemode,
                        fl.defname,
                        NOW() at time zone 'utc',

                        count(*) ""count_total"",
                        count(*) filter (where at.won = true) ""win_total"",

                        count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval)) ""count_month"",
                        count(*) filter (where at.won = true AND m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval)) ""win_month"",

                        count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval)) ""count_week"",
                        count(*) filter (where at.won = true AND m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval)) ""win_week"",

                        count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval)) ""count_day"",
                        count(*) filter (where at.won = true AND m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval)) ""win_day""
                    FROM
                        first_lab fl
                        left join bar_match m ON fl.id = m.id
                        left join bar_match_player p ON fl.id = p.game_id AND fl.team_id = p.team_id
                        left join bar_match_ally_team at ON fl.id = at.game_id AND p.ally_team_id = at.ally_team_id
                    WHERE m.gamemode <> 0
                    GROUP BY 1, 2, 3;
                ",
                new { MapFileName = mapFilename },
                commandTimeout: 60 * 5,
                cancellationToken: cancel
            ))).ToList();

            if (openers.Count == 0) {
                throw new Exception($"missing any opener labs for map [mapName={mapFilename}]");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, $@"
				BEGIN TRANSACTION;

				DELETE FROM map_stats_opening_lab WHERE map_file_name = @MapFileName;

				INSERT INTO map_stats_opening_lab (
					map_file_name, gamemode, def_name,
					timestamp,
					count_total, win_total,
					count_month, win_month,
					count_week, win_week,
					count_day, win_day
				) VALUES 
                    {string.Join(",\n", openers.Select((iter, index) =>
                        $@"(@MapFileName, @Gamemode{index}, @DefName{index}, NOW() at time zone 'utc',
                        @CountTotal{index}, @WinTotal{index}, 
                        @CountMonth{index}, @WinMonth{index}, 
                        @CountWeek{index}, @WinWeek{index}, 
                        @CountDay{index}, @WinDay{index} )"
                    ))};

				COMMIT TRANSACTION;
			", cancel);

            //_Logger.LogDebug($"{cmd.Print()}");

            cmd.AddParameter("MapFileName", mapFilename);

            for (int i = 0; i < openers.Count; ++i) {
                MapStatsOpeningLab opener = openers[i];
                cmd.AddParameter($"@Gamemode{i}", opener.Gamemode);
                cmd.AddParameter($"@DefName{i}", opener.DefName);
                cmd.AddParameter($"@CountTotal{i}", opener.CountTotal);
                cmd.AddParameter($"@WinTotal{i}", opener.WinTotal);
                cmd.AddParameter($"@CountMonth{i}", opener.CountMonth);
                cmd.AddParameter($"@WinMonth{i}", opener.WinMonth);
                cmd.AddParameter($"@CountWeek{i}", opener.CountWeek);
                cmd.AddParameter($"@WinWeek{i}", opener.WinWeek);
                cmd.AddParameter($"@CountDay{i}", opener.CountDay);
                cmd.AddParameter($"@WinDay{i}", opener.WinDay);
            }

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task Generate(MapOpeningLabNeedsUpdate entry, CancellationToken cancel) {
            using NpgsqlConnection evConn = _DbHelper.Connection(Dbs.EVENT);

            List<MapStatsOpeningLab> openers = (await evConn.QueryAsync<MapStatsOpeningLab>(new CommandDefinition(
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
                        m.map_name,
                        m.gamemode,
                        fl.defname,
                        NOW() at time zone 'utc',

                        count(*) ""count_total"",
                        count(*) filter (where at.won = true) ""win_total"",

                        count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval)) ""count_month"",
                        count(*) filter (where at.won = true AND m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval)) ""win_month"",

                        count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval)) ""count_week"",
                        count(*) filter (where at.won = true AND m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval)) ""win_week"",

                        count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval)) ""count_day"",
                        count(*) filter (where at.won = true AND m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval)) ""win_day""
                    FROM
                        first_lab fl
                        left join bar_match m ON fl.id = m.id
                        left join bar_match_player p ON fl.id = p.game_id AND fl.team_id = p.team_id
                        left join bar_match_ally_team at ON fl.id = at.game_id AND p.ally_team_id = at.ally_team_id
                    WHERE m.gamemode <> 0
                    GROUP BY 1, 2, 3;
                ",
                new { MapFileName = entry.MapFilename },
                commandTimeout: 60 * 5,
                cancellationToken: cancel
            ))).ToList();

            if (openers.Count == 0) {
                throw new Exception($"missing any opener labs for map [mapName={entry.MapFilename}]");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, $@"
				BEGIN TRANSACTION;

				DELETE FROM map_stats_opening_lab WHERE map_file_name = @MapFileName;

				INSERT INTO map_stats_opening_lab (
					map_file_name, gamemode, def_name,
					timestamp,
					count_total, win_total,
					count_month, win_month,
					count_week, win_week,
					count_day, win_day
				) VALUES 
                    {string.Join(",\n", openers.Select((iter, index) =>
                        $@"(@MapFileName, @Gamemode{index}, @DefName{index}, NOW() at time zone 'utc',
                        @CountTotal{index}, @WinTotal{index}, 
                        @CountMonth{index}, @WinMonth{index}, 
                        @CountWeek{index}, @WinWeek{index}, 
                        @CountDay{index}, @WinDay{index} )"
                    ))};

				COMMIT TRANSACTION;
			", cancel);

            //_Logger.LogDebug($"{cmd.Print()}");

            cmd.AddParameter("MapFileName", entry.MapFilename);

            for (int i = 0; i < openers.Count; ++i) {
                MapStatsOpeningLab opener = openers[i];
                cmd.AddParameter($"@Gamemode{i}", opener.Gamemode);
                cmd.AddParameter($"@DefName{i}", opener.DefName);
                cmd.AddParameter($"@CountTotal{i}", opener.CountTotal);
                cmd.AddParameter($"@WinTotal{i}", opener.WinTotal);
                cmd.AddParameter($"@CountMonth{i}", opener.CountMonth);
                cmd.AddParameter($"@WinMonth{i}", opener.WinMonth);
                cmd.AddParameter($"@CountWeek{i}", opener.CountWeek);
                cmd.AddParameter($"@WinWeek{i}", opener.WinWeek);
                cmd.AddParameter($"@CountDay{i}", opener.CountDay);
                cmd.AddParameter($"@WinDay{i}", opener.WinDay);
            }

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
