using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMatchRepository {

        private readonly ILogger<BarMatchRepository> _Logger;
        private readonly BarMatchDb _MatchDb;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_ID = "Gex.Match.{0}"; // {0} => game ID

        public BarMatchRepository(ILogger<BarMatchRepository> logger,
            BarMatchDb matchDb, IMemoryCache cache) {

            _Logger = logger;
            _MatchDb = matchDb;
            _Cache = cache;
        }

        public async Task<BarMatch?> GetByID(string gameID) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);

            if (_Cache.TryGetValue(cacheKey, out BarMatch? match) == false) {
                match = await _MatchDb.GetByID(gameID);

                _Cache.Set(cacheKey, match, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return match;
        }

        public async Task<List<BarMatch>> GetRecent(int offset, int limit) {
            return await _MatchDb.GetRecent(offset, limit);
        }

        public async Task<List<BarMatch>> GetByTimePeriod(DateTime start, DateTime end) {
            return await _MatchDb.GetByTimePeriod(start, end);
        }

        public Task Insert(BarMatch match, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, match.ID);
            _Cache.Remove(cacheKey);
            return _MatchDb.Insert(match, cancel);
        }

    }
}
