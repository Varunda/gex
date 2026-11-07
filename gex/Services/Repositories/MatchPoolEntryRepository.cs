using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class MatchPoolEntryRepository {

        private readonly ILogger<MatchPoolEntryRepository> _Logger;
        private readonly MatchPoolEntryDb _Db;

        private const string CACHE_KEY_BY_POOL = "Gex.MatchPoolEntry.Pool.{0}"; // {0} => pool ID
        private const string CACHE_KEY_BY_MATCH = "Gex.MatchPoolEntry.Match.{0}"; // {0} => game ID
        private readonly IMemoryCache _Cache;

        public MatchPoolEntryRepository(ILogger<MatchPoolEntryRepository> logger,
            MatchPoolEntryDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<MatchPoolEntry>> GetByPoolID(long poolID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_BY_POOL, poolID);

            if (_Cache.TryGetValue(cacheKey, out List<MatchPoolEntry>? entries) == false || entries == null) {
                entries = await _Db.GetByPoolID(poolID, cancel);

                _Cache.Set(cacheKey, entries, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
            }

            return entries;
        }

        /// <summary>
        ///     get a list of all <see cref="MatchPoolEntry"/>s a match is a part of
        /// </summary>
        /// <param name="matchID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<MatchPoolEntry>> GetByMatchID(string matchID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_BY_MATCH, matchID);

            if (_Cache.TryGetValue(cacheKey, out List<MatchPoolEntry>? entries) == false || entries == null) {
                entries = await _Db.GetByMatchID(matchID, cancel);

                _Cache.Set(cacheKey, entries, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
            }

            return entries;
        }

        public async Task<List<MatchPoolEntry>> GetByMatchIDs(IEnumerable<string> matchIDs, CancellationToken cancel) {
            List<string> gameIDs = matchIDs.ToList();

            List<MatchPoolEntry> ret = [];
            foreach (string gameID in gameIDs.ToList()) {
                string cacheKey = string.Format(CACHE_KEY_BY_MATCH, gameID);

                if (_Cache.TryGetValue(cacheKey, out List<MatchPoolEntry>? entries) == true && entries != null) {
                    gameIDs.Remove(gameID);
                    ret.AddRange(entries);
                }
            }

            if (gameIDs.Count != 0) {
                List<MatchPoolEntry> entries = await _Db.GetByMatchIDs(gameIDs, cancel);

                Dictionary<string, List<MatchPoolEntry>> dict = [];
                foreach (MatchPoolEntry entry in entries) {
                    List<MatchPoolEntry> entryList = dict.GetValueOrDefault(entry.MatchID) ?? [];
                    entryList.Add(entry);
                    dict[entry.MatchID] = entryList;
                }

                foreach (KeyValuePair<string, List<MatchPoolEntry>> kvp in dict) {
                    ret.AddRange(kvp.Value);

                    string cacheKey = string.Format(CACHE_KEY_BY_MATCH, kvp.Key);
                    _Cache.Set(cacheKey, kvp.Value, new MemoryCacheEntryOptions() {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                    });
                }
            }

            return ret;
        }

        public async Task<MatchPoolEntry?> GetByPoolAndMatchID(long poolID, string matchID, CancellationToken cancel) {
            return await _Db.GetByPoolAndMatchID(poolID, matchID, cancel);
        }

        /// <summary>
        ///     insert a new <see cref="MatchPoolEntry"/> into the DB
        /// </summary>
        /// <param name="entry">entry to insert</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task Insert(MatchPoolEntry entry, CancellationToken cancel) {
            _Cache.Remove(string.Format(CACHE_KEY_BY_POOL, entry.PoolID));
            _Cache.Remove(string.Format(CACHE_KEY_BY_MATCH, entry.MatchID));

            return _Db.Insert(entry, cancel);
        }

        /// <summary>
        ///     update the <see cref="MatchPoolEntry.Description"/> of an entry
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task UpdateDescription(MatchPoolEntry entry, CancellationToken cancel) {
            _Cache.Remove(string.Format(CACHE_KEY_BY_POOL, entry.PoolID));
            _Cache.Remove(string.Format(CACHE_KEY_BY_MATCH, entry.MatchID));

            return _Db.UpdateDescription(entry, cancel);
        }

        public Task Remove(MatchPoolEntry entry, CancellationToken cancel) {
            _Cache.Remove(string.Format(CACHE_KEY_BY_POOL, entry.PoolID));
            _Cache.Remove(string.Format(CACHE_KEY_BY_MATCH, entry.MatchID));

            return _Db.Remove(entry, cancel);
        }

    }
}
