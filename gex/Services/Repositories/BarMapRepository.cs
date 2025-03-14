using gex.Models;
using gex.Models.Bar;
using gex.Services.BarApi;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMapRepository {

        private readonly ILogger<BarMapRepository> _Logger;
        private readonly BarMapApi _Api;
        private readonly BarMapDb _Db;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_NAME = "Gex.BarMap.{0}"; // {0} => map name

        public BarMapRepository(ILogger<BarMapRepository> logger,
            BarMapApi api, BarMapDb db,
            IMemoryCache cache) {

            _Logger = logger;
            _Api = api;
            _Db = db;
            _Cache = cache;
        }

        public async Task<BarMap?> GetByName(string filename, CancellationToken cancel) {

            string cacheKey = string.Format(CACHE_KEY_NAME, filename);
            if (_Cache.TryGetValue(cacheKey, out BarMap? map) == false) {
                map = await _Db.GetByFileName(filename);

                if (map == null) {
                    Result<BarMap, string> api = await _Api.GetByName(filename, cancel);

                    if (api.IsOk == true) {
                        await _Db.Upsert(api.Value);
                    } else {
                        _Logger.LogWarning($"failed to load map from API [filename={filename}] [error={api.Error}]");
                    }
                }

                _Cache.Set(cacheKey, map, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(3)
                });
            }

            return map;
        }

    }
}
