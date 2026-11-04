using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Repository.Match {

    public class BarMatchPlayerRepository {

        private readonly ILogger<BarMatchPlayerRepository> _Logger;
        private readonly IBarMatchPlayerDb _Db;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ID = "Gex.MatchPlayers.{0}"; // {0} => game ID

        private const string CACHE_KEY_SEARCH_PLAYER = "Gex.MatchPlayers.Search.{0}"; // {0} => input

        public BarMatchPlayerRepository(ILogger<BarMatchPlayerRepository> logger,
            IBarMatchPlayerDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);

            if (_Cache.TryGetValue(cacheKey, out List<BarMatchPlayer>? players) == false || players == null) {
                players = await _Db.GetByGameID(gameID, cancel);

                _Cache.Set(cacheKey, players, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return players;
        }

        public async Task<List<BarMatchPlayer>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            if (!IDs.Any()) {
                return new List<BarMatchPlayer>();
            }

            List<BarMatchPlayer> p = [];

            List<string> localIDs = new(IDs);

            // the .toList here is to make a copy, so its safe to remove entries from localIDs
            foreach (string ID in localIDs.ToList()) {
                cancel.ThrowIfCancellationRequested();

                string cacheKey = string.Format(CACHE_KEY_ID, ID);
                if (_Cache.TryGetValue(cacheKey, out List<BarMatchPlayer>? matchPlayers) == true && matchPlayers != null) {
                    localIDs.Remove(ID);
                    p.AddRange(matchPlayers);
                }
            }

            if (localIDs.Count > 0) {
                List<BarMatchPlayer> dbMatches = await _Db.GetByGameIDs(localIDs, cancel);
                p.AddRange(dbMatches);
            }

            IEnumerable<IGrouping<string, BarMatchPlayer>> players = p.GroupBy(iter => iter.GameID);
            foreach (IGrouping<string, BarMatchPlayer> groupedPlayers in players) {
                string cacheKey = string.Format(CACHE_KEY_ID, groupedPlayers.Key);
                _Cache.Set(cacheKey, groupedPlayers.ToList(), new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return p;
        }

        /// <summary>
        ///     searc hthe <see cref="BarMatchPlayer"/>s by name
        /// </summary>
        /// <param name="name">name. minimumum 2 characters</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<List<BarMatchPlayer>> SearchByName(string name, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(name) == true) {
                throw new ArgumentException($"{nameof(name)} cannot be null or empty");
            }

            if (name.Length <= 2) {
                throw new ArgumentException($"{nameof(name)} must be at least 3 characters long");
            }

            string cacheKey = string.Format(CACHE_KEY_SEARCH_PLAYER, name);
            if (_Cache.TryGetValue(cacheKey, out List<BarMatchPlayer>? players) == false || players == null) {
                players = await _Db.SearchByName(name, cancel);

                _Cache.Set(cacheKey, players, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return players;
        }

        public Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel) {
            return _Db.GetByUserID(userID, cancel);
        }

        public Task<List<BarMatchPlayer>> GetAll(CancellationToken cancel) {
            return _Db.GetAll(cancel);
        }

        public async Task Insert(BarMatchPlayer player) {
            string cacheKey = string.Format(CACHE_KEY_ID, player.GameID);
            _Cache.Remove(cacheKey);
            await _Db.Insert(player);
        }

        public async Task DeleteByGameID(string gameID) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);
            _Cache.Remove(cacheKey);
            await _Db.DeleteByGameID(gameID);
        }

    }
}
