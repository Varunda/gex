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

    public class BarMatchPlayerRepository {

        private readonly ILogger<BarMatchPlayerRepository> _Logger;
        private readonly BarMatchPlayerDb _Db;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ID = "Gex.MatchPlayers.{0}"; // {0} => game ID

        public BarMatchPlayerRepository(ILogger<BarMatchPlayerRepository> logger,
            BarMatchPlayerDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);

            if (_Cache.TryGetValue(cacheKey, out List<BarMatchPlayer>? players) == false || players == null) {
                players = await _Db.GetByGameID(gameID, cancel);

                _Cache.Set(cacheKey, players, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
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
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return p;
        }

        public Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel) {
            return _Db.GetByUserID(userID, cancel);
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
