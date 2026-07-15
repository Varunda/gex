using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Models.Map;
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
        private const string CACHE_KEY_UNIQUE_COLORS = "Gex.MatchPlayers.UniqueColors"; // game ID cannot contain these characters, no collision

        private static readonly SemaphoreSlim _Lock = new(1, 1);

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

        public Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel) {
            return _Db.GetByUserID(userID, cancel);
        }

        public async Task<HashSet<int>> GetUniqueColors(CancellationToken cancel) {
            _Lock.Wait(cancel);

            if (_Cache.TryGetValue(CACHE_KEY_UNIQUE_COLORS, out HashSet<int>? colors) == false || colors == null) {
                _Logger.LogDebug($"loading unique colors from DB");

                colors = new HashSet<int>(await _Db.GetUniqueColors(cancel));
                colors.AddRange(_LutColors);
                // add the faction colors used in some places
                colors.Add(0x487edb); // Armada
                colors.Add(0xb93d32); // Cortex
                colors.Add(0x93c034); // Legion
                colors.Add(0xaaaaaa); // Random

                _Cache.Set(CACHE_KEY_UNIQUE_COLORS, colors, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
            }

            _Lock.Release();

            return colors;
        }

        public async Task Insert(BarMatchPlayer player) {
            string cacheKey = string.Format(CACHE_KEY_ID, player.GameID);
            _Cache.Remove(cacheKey);
            await _Db.Insert(player);
        }

        public async Task UpdateStartSpotRole(StartSpotSideStartRoleOverride @override, CancellationToken cancel) {
            await _Db.UpdateStartSpotRole(@override, cancel);
        }

        public async Task DeleteByGameID(string gameID) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);
            _Cache.Remove(cacheKey);
            await _Db.DeleteByGameID(gameID);
        }

        /// <summary>
        ///     this is every color found in src/Lut.ts, which changes the default colors to something a bit nicer
        /// </summary>
        private static readonly HashSet<int> _LutColors = new HashSet<int>([
            4882911, 12205619, 4300105, 14989128, 13456512, 6140338, 13593897, 15106985,
            5866556, 10977351, 8824313, 9517358, 7389575, 13403693, 11311343, 4688543, 9682996,
            13724862, 11323618, 7773002, 10964534, 14655326, 5149392, 13400391, 14063573, 5525888,
            8146224, 10927475, 9517358, 9022016, 4617685, 12205619, 4300105, 15440252, 6140338,
            15520136, 5866556, 15106985, 13456512, 7389575, 8599079, 8824313, 5208436, 10964534,
            9682996, 8146224, 11323618, 7773002, 4688543, 14989128, 9254472, 5525888, 7228234,
            6640038, 4882911, 4617685, 4300105, 6542022, 7430366, 9030017, 3894583, 8430042, 9077463,
            4688543, 7773002, 6640038, 4871861, 6908829, 11323618, 5149392, 9022016, 5525888, 10927475,
            11311343, 4747072, 12205619, 14989128, 13593897, 13456512, 15520136, 8406081, 15106985, 10977351,
            10964534, 14655326, 9852530, 8146224, 13403693, 11969165, 9254472, 13724862, 13400391, 14063573,
            7228234, 9517358, 4882911, 6140338, 8824313, 4871861, 11323618, 4688543, 5346770, 6317711, 12205619,
            13593897, 14989128, 13527899, 14655326, 10977351, 13403693, 9517358, 4300105, 9682996, 5866556, 7389575,
            7773002, 9022016, 10927475, 4747072, 4882911, 8824313, 11323618, 6140338, 5149392, 4688543, 12205619,
            13593897, 13527899, 10964534, 13456512, 9254472, 4300105, 9682996, 5866556, 7389575, 7773002, 9022016,
            14989128, 13403693, 15520136, 14655326, 11969165, 10977351, 4882911, 8824313, 11323618, 6140338, 5149392,
            12205619, 13593897, 13527899, 10964534, 9517358, 4300105, 9682996, 5866556, 7389575, 7773002, 14989128, 13403693,
            15520136, 14655326, 10977351, 13456512, 13724862, 14063573, 10566808, 6826851, 4882911, 8824313, 11323618,
            4871861, 12205619, 13527899, 10964534, 9517358, 4300105, 9682996, 5866556, 7389575, 14989128, 13403693,
            15520136, 9528350, 13456512, 13724862, 14063573, 9254472, 13593897, 14655326, 13400391, 8146224,
            4882911, 8824313, 4871861, 12205619, 13527899, 9517358, 4300105, 9682996, 5866556, 14989128, 13403693,
            15520136, 13456512, 13724862, 14063573, 13593897, 14655326, 13400391, 6140338, 4688543, 11323618,
            4882911, 8824313, 4871861, 12205619, 13527899, 9517358, 4300105, 9682996, 5866556, 14989128,
            13403693, 15520136, 13456512, 13724862, 9254472, 13593897, 14655326, 13400391, 6140338, 4688543,
            11323618, 9135587, 7159967, 11311343,
        ]);

    }
}
