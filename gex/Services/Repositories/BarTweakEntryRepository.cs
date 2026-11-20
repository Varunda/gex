using gex.Models.Bar;
using gex.Services.Db.Bar;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarTweakEntryRepository {

        private readonly ILogger<BarTweakEntryRepository> _Logger;
        private readonly BarTweakEntryDb _Db;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_ALL = "Gex.TweakEntry.All";

        public BarTweakEntryRepository(ILogger<BarTweakEntryRepository> logger,
            BarTweakEntryDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<BarTweakEntry>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out List<BarTweakEntry>? entries) == false || entries == null) {
                entries = await _Db.GetAll(cancel);

                _Cache.Set(CACHE_KEY_ALL, entries, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });
            }

            return entries;
        }

    }
}
