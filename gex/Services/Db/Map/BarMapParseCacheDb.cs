using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Map;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class BarMapParseCacheDb {

        private readonly ILogger<BarMapParseCacheDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMapParseCacheDb(ILogger<BarMapParseCacheDb> logger, IDbHelper dbHelper) {
            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get the cached <see cref="BarMapData"/> by map filename
        /// </summary>
        /// <param name="mapFilename">filename of the map</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<BarMapData?> GetByMapFilename(string mapFilename, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                SELECT data FROM bar_map_parse_cache
                    WHERE map_filename = @MapFilename;
            ", cancel);

            cmd.AddParameter("MapFilename", mapFilename);
            await cmd.PrepareAsync(cancel);

            DbDataReader reader = await cmd.ExecuteReaderAsync(cancel);
            if (reader.Read() == false) {
                return null;
            }

            JsonElement json = reader.GetJsonb("data");
            await conn.CloseAsync();

            BarMapData? data = JsonSerializer.Deserialize<BarMapData>(json);

            return data;
        }

        public async Task<List<string>> GetAllMapFilenames(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                SELECT map_filename FROM bar_map_parse_cache;
            ", cancel);

            List<string> ret = [];


            DbDataReader reader = await cmd.ExecuteReaderAsync(cancel);
            while (reader.Read() == true) {
                ret.Add(reader.GetString(0));
            }

            return ret;
        }

        /// <summary>
        ///     upsert (insert or update) a <see cref="BarMapData"/> into the parse cache
        /// </summary>
        /// <param name="mapFilename">filename of the map</param>
        /// <param name="data">data that will be serialized into JSON and cached</param>
        /// <param name="cancel">cancellation token</param>
        public async Task Upsert(string mapFilename, BarMapData data, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_map_parse_cache (
                    map_filename, timestamp, data
                ) VALUES (
                    @MapFilename, NOW() at time zone 'utc', @Data
                ) ON CONFLICT (map_filename) DO UPDATE
                    SET data = @Data;
            ", cancel);

            cmd.AddParameter("MapFilename", mapFilename);
            cmd.AddParameter("Data", JsonSerializer.SerializeToElement(data));
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
