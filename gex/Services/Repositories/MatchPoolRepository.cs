using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class MatchPoolRepository {

        private readonly ILogger<MatchPoolRepository> _Logger;
        private readonly MatchPoolDb _MatchPoolDb;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ALL = "Gex.MatchPool.All";

        public MatchPoolRepository(ILogger<MatchPoolRepository> logger,
            MatchPoolDb matchPoolDb, IMemoryCache cache) {

            _Logger = logger;
            _MatchPoolDb = matchPoolDb;
            _Cache = cache;
        }

        public async Task<List<MatchPool>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out List<MatchPool>? pools) == false || pools == null) {
                pools = await _MatchPoolDb.GetAll(cancel);

                _Cache.Set(CACHE_KEY_ALL, pools, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return pools;
        }

        public async Task<MatchPool?> GetByID(long poolID, CancellationToken cancel) {
            return await _MatchPoolDb.GetByID(poolID, cancel);
        }

        public async Task<long> Create(MatchPool pool, CancellationToken cancel) {
            _Cache.Remove(CACHE_KEY_ALL);
            return await _MatchPoolDb.Create(pool, cancel);
        }

        public async Task UpdateName(long poolID, MatchPool pool, CancellationToken cancel) {
            _Cache.Remove(CACHE_KEY_ALL);
            await _MatchPoolDb.UpdateName(poolID, pool, cancel);
        }

    }
}
