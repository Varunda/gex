using gex.Models.MapStats;
using gex.Services.Db.MapStats;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class MapStatsStartSpotRepository {

        private readonly ILogger<MapStatsStartSpotRepository> _Logger;
        private readonly MapStatsStartSpotDb _Db;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_BY_USER = "Gex.MapStats.User.StartSpot.{0}.{1}"; // {0} => map filename, {1} => user ID

        public MapStatsStartSpotRepository(ILogger<MapStatsStartSpotRepository> logger,
            MapStatsStartSpotDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public Task<List<MapStatsStartSpot>> GetByMap(string mapFilename, CancellationToken cancel) {
            return _Db.GetByMap(mapFilename, cancel);
        }

        public Task Generate(string mapFilename, CancellationToken cancel) {
            return _Db.Generate(mapFilename, cancel);
        }

        public async Task<List<MapStatsStartSpot>> GetByMapAndUser(string mapFilename, long userID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_BY_USER, mapFilename, userID);
            if (_Cache.TryGetValue(cacheKey, out List<MapStatsStartSpot>? startSpots) == false || startSpots == null) {
                startSpots = await _Db.GetByMapAndUser(mapFilename, userID, cancel);

                _Cache.Set(cacheKey, startSpots, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return startSpots;
        }

    }
}
