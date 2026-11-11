using gex.Common.Models;
using gex.Models.Apm;
using gex.Services.BarApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class ApmTournamentRepository {

        private readonly ILogger<ApmTournamentRepository> _Logger;
        private readonly ApmWebsiteApi _Api;

        private const string CACHE_KEY_ENTRIES = "Gex.ApmTournament.Entries";
        private const string CACHE_KEY_INFO = "Gex.ApmTourament.Info.{0}"; // {0} => tourney ID

        private readonly IMemoryCache _Cache;

        public ApmTournamentRepository(ILogger<ApmTournamentRepository> logger,
            ApmWebsiteApi api, IMemoryCache cache) {

            _Logger = logger;
            _Api = api;
            _Cache = cache;
        }

        /// <summary>
        ///     get all <see cref="ApmTournamentEntry"/>s, paginating if needed to get them all
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Result<List<ApmTournamentEntry>, string>> GetEntries(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ENTRIES, out Result<List<ApmTournamentEntry>, string>? entries) == false || entries == null) {
                _Logger.LogDebug($"loading all tournament entries from apm");
                List<ApmTournamentEntry> allEntries = [];

                bool ok = true;
                for (int i = 1; i < 1000; ++i) {
                    Result<List<ApmTournamentEntry>, string> ret = await _Api.GetEntries(i, cancel);

                    if (ret.IsOk == false) {
                        entries = ret;
                        ok = false;
                        break;
                    }

                    allEntries.AddRange(ret.Value);

                    if (ret.Value.Count < 100) {
                        break;
                    }
                }

                if (ok == true) {
                    _Logger.LogDebug($"loaded all tournament entries from apm [count={allEntries.Count}]");
                    entries = Result<List<ApmTournamentEntry>, string>.Ok(allEntries);
                    _Cache.Set(CACHE_KEY_ENTRIES, entries, new MemoryCacheEntryOptions() {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                } else {
                    _Cache.Set(CACHE_KEY_ENTRIES, entries, new MemoryCacheEntryOptions() {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                    });
                }
            }

            if (entries == null) {
                throw new InvalidOperationException($"how is entries null here?");
            }

            return entries;
        }

        /// <summary>
        ///     get the <see cref="ApmTournamentInfo"/> for a specific tournament
        /// </summary>
        /// <param name="id">ID of the tournament to get the info of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<ApmTournamentInfo, string>> GetTournamentInfo(string id, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_INFO, id);

            if (_Cache.TryGetValue(cacheKey, out Result<ApmTournamentInfo, string>? info) == false || info == null) {
                info = await _Api.GetTournamentInfo(id, cancel);

                _Cache.Set(cacheKey, info, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = info.IsOk == true ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(1)
                });
            }

            return info;
        }

    }
}
