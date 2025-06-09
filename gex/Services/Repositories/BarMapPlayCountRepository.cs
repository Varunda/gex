using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMapPlayCountRepository {

        private readonly ILogger<BarMapPlayCountRepository> _Logger;
        private readonly BarMapPlayCountDb _Db;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY = "Gex.MapPlayCount";

        public BarMapPlayCountRepository(ILogger<BarMapPlayCountRepository> logger,
            BarMapPlayCountDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<BarMapPlayCountEntry>> Get(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY, out List<BarMapPlayCountEntry>? entries) == false || entries == null) {
                entries = await _Db.Get(cancel);

                _Cache.Set(CACHE_KEY, entries, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return entries;
        }

    }
}
