using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

	public class SkillHistogramRepository {

		private readonly ILogger<SkillHistogramRepository> _Logger;
		private readonly SkillHistogramDb _Db;

		private readonly IMemoryCache _Cache;

		private const string CACHE_KEY = "Gex.SkillHistogram";

		public SkillHistogramRepository(ILogger<SkillHistogramRepository> logger,
			SkillHistogramDb db, IMemoryCache cache) {

			_Logger = logger;
			_Db = db;
			_Cache = cache;
		}

		public async Task<List<SkillHistogramEntry>> Get(CancellationToken cancel) {
			if (_Cache.TryGetValue(CACHE_KEY, out List<SkillHistogramEntry>? entries) == false || entries == null) {
				entries = await _Db.Get(cancel);

				_Cache.Set(CACHE_KEY, entries, new MemoryCacheEntryOptions() {
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
				});
			}

			return entries;
		}

	}
}
