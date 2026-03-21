using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

    public class MapStatsDailyUnitsMadeDb {

        private readonly ILogger<MapStatsDailyUnitsMadeDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapStatsDailyUnitsMadeDb(ILogger<MapStatsDailyUnitsMadeDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MapStatsDailyUnitsMade>> GetByMap(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsDailyUnitsMade>(
                "SELECT * FROM map_stats_daily_units_made WHERE map_filename = @MapFilename",
                new { MapFilename = mapFilename },
                cancel
            );
        }

        public async Task Generate(MapStatsNeedsUpdate entry, CancellationToken cancel) {
            using NpgsqlConnection evConn = _DbHelper.Connection(Dbs.EVENT);

            List<MapStatsDailyUnitsMade> openers = (await evConn.QueryAsync<MapStatsDailyUnitsMade>(new CommandDefinition(
                @"
                    WITH map_units AS (
                        SELECT
                            bm.id,
                            guc.definition_name,
                            sum(guc.""count"") ""count""
                        FROM
                            bar_match bm
                            left join game_units_created guc on bm.id = guc.game_id
                        WHERE
                            date_trunc('day', bm.start_time) = @Day
                            AND bm.gamemode = @Gamemode
                            AND bm.map_name = @MapFilename
                        GROUP BY
                            bm.id, guc.definition_name
                    )
                    SELECT
                        m.map_name ""map_filename"",
                        m.gamemode ""gamemode"",
                        mu.definition_name ""definition_name"",
                        date_trunc('day', m.start_time) ""day"",

                        NOW() at time zone 'utc' ""timestamp"",

                        count(*) ""count""
                    FROM
                        map_units mu
                        left join bar_match m ON mu.id = m.id
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

				DELETE FROM map_stats_daily_units_made WHERE map_filename = @MapFileName AND gamemode = @Gamemode AND day = @Day;

				INSERT INTO map_stats_daily_units_made (
					map_filename, gamemode, day,
                    definition_name, count, 
					timestamp
				) VALUES 
                    {string.Join(",\n", openers.Select((iter, index) =>
                        $@"(@MapFileName, @Gamemode, @Day,
                            @DefName{index}, @Count{index},
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
                MapStatsDailyUnitsMade opener = openers[i];
                cmd.AddParameter($"@DefName{i}", opener.DefinitionName);
                cmd.AddParameter($"@Count{i}", opener.Count);
            }

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
