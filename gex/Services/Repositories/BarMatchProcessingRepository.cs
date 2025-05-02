using gex.Models.Db;
using gex.Services.Db.Match;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

	public class BarMatchProcessingRepository {

		private readonly ILogger<BarMatchProcessingRepository> _Logger;
		private readonly BarMatchProcessingDb _ProcessingDb;
		private readonly IMemoryCache _Cache;

		private const string CACHE_KEY = "Gex.MatchProcessing.{0}"; // {0} => game ID
		private const string CACHE_KEY_PRIO = "Gex.MatchProcessing.PriorityList";

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

			// if processing match was cached, and the priority changed, remove the prio list cached
			if (_Cache.TryGetValue(cacheKey, out BarMatchProcessing? cached) == true && cached != null) {
				if (cached.Priority != proc.Priority) {
					_Logger.LogWarning($"priority changed on game, evicting priority list cache [gameID={proc.GameID}] [priority={cached.Priority} -> {proc.Priority}]");
					_Cache.Remove(CACHE_KEY_PRIO);
				}
			} else {
				// if there was cached entry, lets assume this is a new entry, so the prio list might have changed
				_Cache.Remove(CACHE_KEY_PRIO);
			}

            _Cache.Remove(cacheKey);

            return _ProcessingDb.Upsert(proc);
        }

		/// <summary>
		///		get a <see cref="BarMatchProcessing"/> by its <see cref="BarMatchProcessing.GameID"/>
		/// </summary>
		/// <param name="gameID">ID of the game</param>
		/// <param name="cancel">cancellation token</param>
		/// <returns>
		///		the <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/> of <paramref name="gameID"/>,
		///		or <c>nul</c> if it does not exist
		/// </returns>
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

		/// <summary>
		///		get a list of all <see cref="BarMatchProcessing"/> entries that are pending processing.
		///		used during start up to re-queue everything that was in a queue
		/// </summary>
		/// <param name="cancel">cancellation token</param>
		/// <returns></returns>
        public Task<List<BarMatchProcessing>> GetPending(CancellationToken cancel) {
            return _ProcessingDb.GetPending(cancel);
        }

		/// <summary>
		///		get the lowest priority <see cref="BarMatchProcessing"/> entry, which represents the
		///		next game in the priority list of games to be ran locally
		/// </summary>
		/// <param name="cancel">cancellation token</param>
		/// <returns>
		///		the lowest priority <see cref="BarMatchProcessing"/>, or <c>null</c> if none exists
		/// </returns>
		public Task<BarMatchProcessing?> GetLowestPriority(CancellationToken cancel) {
			return _ProcessingDb.GetLowestPriority(cancel);
		}

		/// <summary>
		///		get the list of <see cref="BarMatchProcessing"/>s entries, ordered by lowest <see cref="BarMatchProcessing.Priority"/>
		///		first, then sooner games (based on <see cref="BarMatch.StartTime"/>)
		/// </summary>
		/// <param name="cancel">cancellation token</param>
		/// <returns>
		///		an ordered list that represents the priority list of games to be processed
		/// </returns>
		public async Task<List<BarMatchProcessing>> GetPriorityList(CancellationToken cancel) {
			if (_Cache.TryGetValue(CACHE_KEY_PRIO, out List<BarMatchProcessing>? list) == false || list == null) {
				list = await _ProcessingDb.GetPriorityList(100, cancel);

				_Cache.Set(CACHE_KEY_PRIO, list, new MemoryCacheEntryOptions() {
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
				});
			}

			return list;
		}

	}
}
