using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class MapEngineUsageDb {

        private readonly ILogger<MapEngineUsageDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapEngineUsageDb(ILogger<MapEngineUsageDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Upsert(MapEngineUsage usage, CancellationToken cancel) {
            if (string.IsNullOrEmpty(usage.Engine)) {
                throw new Exception($"missing {nameof(MapEngineUsage.Engine)} from {nameof(MapEngineUsage)}");
            }
            if (string.IsNullOrEmpty(usage.Map)) {
                throw new Exception($"missing {nameof(MapEngineUsage.Map)} from {nameof(MapEngineUsage)}");
            }
            if (usage.LastUsed == default) {
                throw new Exception($"default {nameof(MapEngineUsage.LastUsed)} from {nameof(MapEngineUsage)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO map_engine_usage AS v (
                    engine, map, last_used, deleted_on
                ) VALUES (
                    @Engine, @Map, @LastUsed, @DeletedOn
                ) ON CONFLICT (engine, map) DO UPDATE SET
                    last_used = @LastUsed,
                    deleted_on = @DeletedOn
                WHERE v.last_used < @LastUsed;
            ", cancel);

            cmd.AddParameter("Engine", usage.Engine);
            cmd.AddParameter("Map", usage.Map);
            cmd.AddParameter("LastUsed", usage.LastUsed);
            cmd.AddParameter("DeletedOn", usage.DeletedOn);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task MarkDeleted(MapEngineUsage usage, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE map_engine_usage
                    SET deleted_on = @DeletedOn
                WHERE
                    engine = @Engine
                    AND map = @Map;
            ", cancel);

            cmd.AddParameter("Engine", usage.Engine);
            cmd.AddParameter("Map", usage.Map);
            cmd.AddParameter("DeletedOn", usage.DeletedOn);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     get all expired <see cref="MapEngineUsage"/>s that mean a map can be deleted from a engine
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<MapEngineUsage>> GetExpired(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapEngineUsage>(
                "SELECT * FROM map_engine_usage WHERE deleted_on IS NULL AND (last_used < NOW() at time zone 'utc' - '2 hour'::interval)",
                cancel
            );
        }

    }
}
