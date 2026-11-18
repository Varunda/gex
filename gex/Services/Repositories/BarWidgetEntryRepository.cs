using gex.Common.Models.Bar;
using gex.Services.Db.Bar;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarWidgetEntryRepository {

        private readonly ILogger<BarWidgetEntryRepository> _Logger;
        private readonly BarWidgetEntryDb _WidgetEntryDb;

        private const string CACHE_KEY_ALL = "Gex.WidgetEntry.All";
        private readonly IMemoryCache _Cache;

        public BarWidgetEntryRepository(ILogger<BarWidgetEntryRepository> logger,
            BarWidgetEntryDb widgetEntryDb, IMemoryCache cache) {

            _Logger = logger;

            _WidgetEntryDb = widgetEntryDb;
            _Cache = cache;
        }

        public async Task<List<BarWidgetEntry>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out List<BarWidgetEntry>? entries) == false || entries == null) {
                entries = await _WidgetEntryDb.GetAll(cancel);

                _Cache.Set(CACHE_KEY_ALL, entries, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return entries;
        }

        public async Task Upsert(BarWidgetEntry entry, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(entry.Hash)) {
                throw new ArgumentException($"{nameof(BarWidgetEntry.Hash)} is missing");
            }

            BarWidgetEntry? existingEntry = (await GetAll(cancel)).FirstOrDefault(iter => iter.Hash == entry.Hash);
            if (existingEntry != null && existingEntry.Name != entry.Name) {
                Debug.Fail($"md5 collision found for widget? [entry={entry.Hash}/{entry.Name}] [existing={existingEntry.Hash}/{existingEntry.Name}]");
            }

            if (existingEntry != null) {
                return;
            }

            await _WidgetEntryDb.Insert(entry, cancel);
            _Logger.LogDebug($"new widget entry found [hash={entry.Hash}] [name={entry.Name}]");
            _Cache.Remove(CACHE_KEY_ALL);
        }

    }
}
