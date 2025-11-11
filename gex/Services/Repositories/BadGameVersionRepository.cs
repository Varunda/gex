using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BadGameVersionRepository {

        private readonly ILogger<BadGameVersionRepository> _Logger;
        private readonly BadGameVersionDb _BadGameVersionDb;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY = "Gex.BadGameVersions.All";

        public BadGameVersionRepository(ILogger<BadGameVersionRepository> logger,
            BadGameVersionDb badGameVersionDb, IMemoryCache cache) {

            _Logger = logger;
            _BadGameVersionDb = badGameVersionDb;
            _Cache = cache;
        }

        public async Task<bool> IsBadGameVersion(string gameVersion, CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY, out HashSet<string>? versions) == false || versions == null) {
                _Logger.LogDebug($"bad game versions are not cached, loading from DB");
                List<BadGameVersion> badVersions = await _BadGameVersionDb.GetAll(cancel);
                versions = new HashSet<string>(badVersions.Select(iter => iter.GameVersion));

                _Cache.Set(CACHE_KEY, versions, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }

            return versions.Contains(gameVersion);
        }

    }
}
