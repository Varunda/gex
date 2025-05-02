using gex.Models.Db;
using gex.Services.Db.Match;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

	public class BarMatchProcessingRepository {

		private readonly ILogger<BarMatchProcessingRepository> _Logger;
		private readonly BarMatchProcessingDb _ProcessingDb;
		private readonly IMemoryCache _Cache;

		private const string CACHE_KEY = "Gex.MatchProcessing.{0}"; // {0} => game ID

		public BarMatchProcessingRepository(ILogger<BarMatchProcessingRepository> logger,
            BarMatchProcessingDb processingDb, IMemoryCache cache) {

			_Logger = logger;
			_ProcessingDb = processingDb;
			_Cache = cache;
		}

        /// <summary>
        ///     update/insert (upsert) a <see cref="BarMatchProcessing"/>
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
		public Task Upsert(BarMatchProcessing proc) {
            string cacheKey = string.Format(CACHE_KEY, proc.GameID);
            _Cache.Remove(cacheKey);

            return _ProcessingDb.Upsert(proc);
        }

        public async Task<BarMatchProcessing?> GetByGameID(string gameID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, gameID);

            if (_Cache.TryGetValue(cacheKey, out BarMatchProcessing? proc) == false) {
                proc = await _ProcessingDb.GetByGameID(gameID, cancel);

                _Cache.Set(cacheKey, proc, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return proc;
        }

        public Task<List<BarMatchProcessing>> GetPending(CancellationToken cancel) {
            return _ProcessingDb.GetPending(cancel);
        }

		public Task<BarMatchProcessing?> GetLowestPriority(CancellationToken cancel) {
			return _ProcessingDb.GetLowestPriority(cancel);
		}

	}
}
