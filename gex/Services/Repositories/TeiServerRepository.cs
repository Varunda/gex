using gex.Common.Models;
using gex.Models.Bar;
using gex.Services.BarApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class TeiServerRepository {

        private readonly ILogger<TeiServerRepository> _Logger;
        private readonly TeiServerApi _Tei;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEASONS = "Gex.TeiServer.Seasons";
        private const string CACHE_KEY_BY_SEASON = "Gex.TeiServer.Season.{0}";  // {0} => season

        public TeiServerRepository(ILogger<TeiServerRepository> logger,
            TeiServerApi tei, IMemoryCache cache) {

            _Logger = logger;
            _Tei = tei;
            _Cache = cache;
        }

        public async Task<Result<List<BarSeason>, string>> GetSeasons(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_SEASONS, out Result<List<BarSeason>, string>? seasons) == false || seasons == null) {
                seasons = await _Tei.GetSeasons(cancel);
                _Cache.Set(CACHE_KEY_SEASONS, seasons, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = seasons.IsOk ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(1)
                });
            }

            return seasons;
        }

        public async Task<Result<List<BarLeaderboard>, string>> GetLeaderboard(int season, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_BY_SEASON, season);

            if (_Cache.TryGetValue(cacheKey, out Result<List<BarLeaderboard>, string>? boards) == false || boards == null) {
                boards = await _Tei.GetLeaderboard(season, cancel);
                _Cache.Set(cacheKey, boards, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = boards.IsOk ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(1)
                });
            }

            return boards;
        }


    }
}
