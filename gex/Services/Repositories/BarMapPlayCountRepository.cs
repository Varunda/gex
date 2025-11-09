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

        private const string CACHE_KEY = "Gex.MapPlayCount.{0}"; // {0} => range

        public BarMapPlayCountRepository(ILogger<BarMapPlayCountRepository> logger,
            BarMapPlayCountDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<BarMapPlayCountEntry>> GetDaily(CancellationToken cancel) {
            return await Get("daily", DateTime.UtcNow - TimeSpan.FromDays(1), cancel);
        }

        public async Task<List<BarMapPlayCountEntry>> Get7Day(CancellationToken cancel) {
            return await Get("7day", DateTime.UtcNow - TimeSpan.FromDays(7), cancel);
        }

        public async Task<List<BarMapPlayCountEntry>> Get30Day(CancellationToken cancel) {
            return await Get("30day", DateTime.UtcNow - TimeSpan.FromDays(30), cancel);
        }

        public Task<List<BarMapPlayCountEntry>> GetWithDate(CancellationToken cancel) {
            return _Db.GetWithDate(DateTime.UtcNow - TimeSpan.FromDays(30), cancel);
        }

        private async Task<List<BarMapPlayCountEntry>> Get(string interval, DateTime rangeStart, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, interval);
            if (_Cache.TryGetValue(cacheKey, out List<BarMapPlayCountEntry>? entries) == false || entries == null) {
                entries = await _Db.Get(rangeStart, cancel);

                _Cache.Set(CACHE_KEY, entries, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return entries;
        }

    }
}
