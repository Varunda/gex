using gex.Models.UserStats;
using gex.Services.Db.UserStats;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarUserUnitsMadeRepository {

        private readonly ILogger<BarUserUnitsMadeRepository> _Logger;
        private readonly BarUserUnitsMadeDb _UnitsMadeDb;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_USER_ID = "Gex.UnitStats.UnitsMade.{0}"; // {0} => user ID

        public BarUserUnitsMadeRepository(ILogger<BarUserUnitsMadeRepository> logger,
            BarUserUnitsMadeDb unitsMadeDb, IMemoryCache cache) {

            _Logger = logger;
            _UnitsMadeDb = unitsMadeDb;
            _Cache = cache;
        }

        public async Task<List<BarUserUnitsMade>> GetByUserID(long userID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_USER_ID, userID);

            if (_Cache.TryGetValue(cacheKey, out List<BarUserUnitsMade>? made) == false || made == null) {
                made = await _UnitsMadeDb.GetByUserID(userID, cancel);

                _Cache.Set(cacheKey, made, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
            }

            return made;
        }

    }
}
