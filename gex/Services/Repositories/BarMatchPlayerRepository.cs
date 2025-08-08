using gex.Models.Db;
using gex.Services.Db.Match;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
