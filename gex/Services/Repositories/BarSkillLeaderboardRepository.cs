using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

	public class BarSkillLeaderboardRepository {

		private readonly ILogger<BarSkillLeaderboardRepository> _Logger;
		private readonly BarSkillLeaderboardDb _LeaderboardDb;

		private readonly IMemoryCache _Cache;

		private const string CACHE_KEY = "Gex.SkillLeaderboard";

		public BarSkillLeaderboardRepository(ILogger<BarSkillLeaderboardRepository> logger,
			BarSkillLeaderboardDb leaderboardDb, IMemoryCache cache) {

			_Logger = logger;

			_LeaderboardDb = leaderboardDb;
			_Cache = cache;
		}

		/// <summary>
		///		load the <see cref="BarSkillLeaderboardEntry"/>s, contains the top players
		///		(sorted by skill) for each gamemode
		/// </summary>
		/// <param name="cancel">cancellation token</param>
		/// <returns></returns>
		public async Task<List<BarSkillLeaderboardEntry>> Get(CancellationToken cancel) {
			if (_Cache.TryGetValue(CACHE_KEY, out List<BarSkillLeaderboardEntry>? entries) == false || entries == null) {
				_Logger.LogDebug($"loading skill leaderboard from DB (not cached)");
				entries = await _LeaderboardDb.Get(cancel);

				_Cache.Set(CACHE_KEY, entries, new MemoryCacheEntryOptions() {
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
				});
			}

			return entries;
		}

	}
}
