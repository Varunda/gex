using gex.Models.Db;
using gex.Services.Db.Match;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMatchRepository {

        private readonly ILogger<BarMatchRepository> _Logger;
        private readonly BarMatchDb _MatchDb;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_ID = "Gex.Match.ID.{0}"; // {0} => game ID
        private const string CACHE_KEY_OLDEST = "Gex.Match.Oldest";
        private const string CACHE_KEY_UNIQUE_ENGINES = "Gex.Match.Unique.Engines";
        private const string CACHE_KEY_UNIQUE_GAME_VERSIONS = "Gex.Match.Unique.GameVersions";
        private const string CACHE_KEY_GAMES_BY_USER = "Gex.Match.User.{0}"; // {0} => user ID

        public BarMatchRepository(ILogger<BarMatchRepository> logger,
            BarMatchDb matchDb, IMemoryCache cache) {

            _Logger = logger;
            _MatchDb = matchDb;
            _Cache = cache;
        }

        public async Task<BarMatch?> GetByID(string gameID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);

            if (_Cache.TryGetValue(cacheKey, out BarMatch? match) == false) {
                match = await _MatchDb.GetByID(gameID, cancel);

                _Cache.Set(cacheKey, match, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return match;
        }

        /// <summary>
        ///     load a list of <see cref="BarMatch"/>s
        /// </summary>
        /// <param name="IDs">list of IDs to load the matches of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetByIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            if (!IDs.Any()) {
                return new List<BarMatch>();
            }

            List<BarMatch> matches = [];

            List<string> localIDs = new(IDs);

            // the .toList here is to make a copy, so its safe to remove entries from localIDs
            foreach (string ID in localIDs.ToList()) {
                cancel.ThrowIfCancellationRequested();

                string cacheKey = string.Format(CACHE_KEY_ID, ID);
                if (_Cache.TryGetValue(cacheKey, out BarMatch? match) == true && match != null) {
                    localIDs.Remove(ID);
                    matches.Add(match);
                }
            }

            if (localIDs.Count > 0) {
                List<BarMatch> dbMatches = await _MatchDb.GetByIDs(localIDs, cancel);
                matches.AddRange(dbMatches);
            }

            foreach (BarMatch m in matches) {
                string cacheKey = string.Format(CACHE_KEY_ID, m.ID);
                _Cache.Set(cacheKey, m, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return matches;
        }

        public Task<List<BarMatch>> GetAll(CancellationToken cancel) {
            return _MatchDb.GetAll(cancel);
        }

        public async Task<List<BarMatch>> GetRecent(int offset, int limit, CancellationToken cancel) {
            return await _MatchDb.GetRecent(offset, limit, cancel);
        }

        public Task<List<BarMatch>> Search(BarMatchSearchParameters parms, int offset, int limit, CancellationToken cancel) {
            return _MatchDb.Search(parms, offset, limit, cancel);
        }

        public async Task<List<BarMatch>> GetByTimePeriod(DateTime start, DateTime end, CancellationToken cancel) {
            return await _MatchDb.GetByTimePeriod(start, end, cancel);
        }

        public async Task<List<BarMatch>> GetByUserID(long userID, CancellationToken cancel) {
            // caching here is mostly used to speed up full user stat fixes
            string cacheKey = string.Format(CACHE_KEY_GAMES_BY_USER, userID);
            if (_Cache.TryGetValue(cacheKey, out List<BarMatch>? matches) == false || matches == null) {
                matches = await _MatchDb.GetByUserID(userID, cancel);

                _Cache.Set(cacheKey, matches, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
            }

            return matches;
        }

        public async Task<BarMatch?> GetOldestMatch(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_OLDEST, out BarMatch? oldest) == false) {
                oldest = await _MatchDb.GetOldestMatch(cancel);

                // cache for a day if found, otherwise just 10 seconds
                _Cache.Set(CACHE_KEY_OLDEST, oldest, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = oldest == null ? TimeSpan.FromSeconds(10) : TimeSpan.FromDays(1)
                });
            }

            return oldest;
        }

        public async Task<List<string>> GetUniqueEngines(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_UNIQUE_ENGINES, out List<string>? list) == false || list == null) {
                list = await _MatchDb.GetUniqueEngines(cancel);

                // TODO: inserting this can probably invalidate this cached value
                _Cache.Set(CACHE_KEY_UNIQUE_ENGINES, list, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }

            return list;
        }

        public async Task<List<string>> GetUniqueGameVersions(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_UNIQUE_GAME_VERSIONS, out List<string>? list) == false || list == null) {
                list = await _MatchDb.GetUniqueGameVersions(cancel);

                // TODO: inserting this can probably invalidate this cached value
                _Cache.Set(CACHE_KEY_UNIQUE_GAME_VERSIONS, list, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }

            return list;
        }

        public Task Insert(BarMatch match, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, match.ID);
            _Cache.Remove(cacheKey);
            return _MatchDb.Insert(match, cancel);
        }

        public Task Delete(string gameID) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);
            _Cache.Remove(cacheKey);
            return _MatchDb.Delete(gameID);
        }

    }
}
