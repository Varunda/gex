using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Options;
using gex.Common.Services.Parser;
using gex.Services.Db.Map;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMapDataRepository {

        private readonly ILogger<BarMapDataRepository> _Logger;

        private readonly BarMapParseCacheDb _ParseCacheDb;
        private readonly BarMapParser _MapParser;
        private readonly IOptions<FileStorageOptions> _StorageOptions;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_FILENAME = "Gex.BarMapData.{0}"; // {0} => map filename

        public BarMapDataRepository(ILogger<BarMapDataRepository> logger,
            BarMapParseCacheDb parseCacheDb, BarMapParser mapParser,
            IMemoryCache cache, IOptions<FileStorageOptions> storageOptions) {

            _Logger = logger;
            _ParseCacheDb = parseCacheDb;
            _MapParser = mapParser;
            _Cache = cache;
            _StorageOptions = storageOptions;
        }

        /// <summary>
        ///     get the <see cref="BarMapData"/> based on map filename
        /// </summary>
        /// <remarks>
        ///     first loads from cache, if missing loads from DB, if missing from DB parses the map
        /// </remarks>
        /// <param name="mapFilename">filename of the map. case-insensitive</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<BarMapData?> GetByMapFilename(string mapFilename, CancellationToken cancel) {
            mapFilename = mapFilename.ToLower();

            string cacheKey = string.Format(CACHE_KEY_FILENAME, mapFilename);

            if (_Cache.TryGetValue(cacheKey, out BarMapData? data) == true) {
                return data;
            }

            _Logger.LogDebug($"missing map data from cache, loading from DB [mapFilename={mapFilename}]");

            data = await _ParseCacheDb.GetByMapFilename(mapFilename, cancel);
            if (data != null) {
                _Cache.Set(cacheKey, data, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });

                return data;
            }

            _Logger.LogDebug($"missing map data from DB, loading from sd7 file [mapFilename={mapFilename}]");

            string mapPath = Path.Join(_StorageOptions.Value.MapLocation, "maps", (mapFilename + ".sd7").EscapeRecoilFilesytemCharacters());
            if (File.Exists(mapPath) == false) {
                return null;
            }

            Result<BarMapData, string> result = await _MapParser.Parse(mapPath, new BarMapParser.ParseOptions() {
                HeightMap = true,
                Header = true,
                Smts = false,
                Metal = true,
                Features = true,
            }, cancel);

            if (result.IsOk == false) {
                _Logger.LogError($"failed to parse map [mapFilename={mapFilename}] [path={mapPath}] [error={result.Error}]");
                return null;
            }

            data = result.Value;

            await _ParseCacheDb.Upsert(mapFilename, data, cancel);
            _Cache.Set(cacheKey, data, new MemoryCacheEntryOptions() {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });

            return data;
        }

        public async Task Upsert(string mapFilename, BarMapData data, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_FILENAME, mapFilename);
            _Cache.Remove(cacheKey);

            await _ParseCacheDb.Upsert(mapFilename, data, cancel);
        }

    }
}
