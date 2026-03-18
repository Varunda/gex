using gex.Code.ExtensionMethods;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

    public class MapStatsNeedsUpdateDb {

        private readonly ILogger<MapStatsNeedsUpdateDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapStatsNeedsUpdateDb(ILogger<MapStatsNeedsUpdateDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get all <see cref="MapStatsNeedsUpdate"/> that are pending generation
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<MapStatsNeedsUpdate>> GetReady(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsNeedsUpdate>(@"
                SELECT *
                FROM map_stats_needs_update
                WHERE last_dirtied <= (NOW() at time zone 'utc' - '2 hours'::interval);
            ", cancel);
        }

        /// <summary>
        ///     get the <see cref="MapStatsNeedsUpdate"/> of a specific map
        /// </summary>
        /// <param name="mapFilename"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<MapStatsNeedsUpdate>> GetByMapFilename(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapStatsNeedsUpdate>(@"
                SELECT *
                FROM map_stats_needs_update
                WHERE map_filename = @MapFilename
            ", new { MapFilename = mapFilename }, cancel);
        }

        /// <summary>
        ///     upsert a <see cref="MapStatsNeedsUpdate"/>
        /// </summary>
        /// <param name="update"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Upsert(MapStatsNeedsUpdate update, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(update.MapFilename)) {
                throw new Exception($"missing {nameof(MapStatsNeedsUpdate.MapFilename)}");
            }
            if (update.Day == default) {
                throw new Exception($"missing {nameof(MapStatsNeedsUpdate.Day)}]");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO map_stats_needs_update (
                    map_filename, gamemode, day, last_dirtied
                ) VALUES (
                    @MapFilename, @Gamemode, @Day, @LastDirtied
                ) ON CONFLICT (map_filename, gamemode, day) DO UPDATE
                    SET last_dirtied = @LastDirtied;
            ", cancel);

            cmd.AddParameter("MapFilename", update.MapFilename);
            cmd.AddParameter("Gamemode", update.Gamemode);
            cmd.AddParameter("Day", update.Day.Date);
            cmd.AddParameter("LastDirtied", update.LastDirtied);

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     remove a <see cref="MapStatsNeedsUpdate"/> from the db, marking it as completed
        /// </summary>
        /// <param name="update"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Remove(MapStatsNeedsUpdate update, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM map_stats_needs_update
                    WHERE map_filename = @MapFilename
                        AND gamemode = @Gamemode
                        AND day = @Day;
            ", cancel);

            cmd.AddParameter("MapFilename", update.MapFilename);
            cmd.AddParameter("Gamemode", update.Gamemode);
            cmd.AddParameter("Day", update.Day);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
