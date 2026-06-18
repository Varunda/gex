using gex.Common.Models;
using gex.Models.Bar;
using gex.Services.BarApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMapRotationRepository {

        private readonly ILogger<BarMapRotationRepository> _Logger;
        private readonly BarMapRotationParser _Parser;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY = "Gex.BarMapRotation.All";

        private const string BASE_URL = "https://raw.githubusercontent.com/beyond-all-reason/spads_config_bar/refs/heads/main/etc/mapLists.conf";

        private static readonly HttpClient _Http = new HttpClient();
        static BarMapRotationRepository() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

        public BarMapRotationRepository(ILogger<BarMapRotationRepository> logger,
            IMemoryCache cache, BarMapRotationParser parser) {

            _Logger = logger;
            _Cache = cache;
            _Parser = parser;
        }

        public async Task<Result<List<BarMapRotation>, string>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY, out List<BarMapRotation>? rotations) == false || rotations == null) {
                HttpResponseMessage response = await _Http.GetAsync(BASE_URL, cancel);
                if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                    _Logger.LogError($"got non-200 OK response code from url [statusCode={response.StatusCode}]");
                    return "got a non-200 OK response code from url";
                }

                string body = await response.Content.ReadAsStringAsync(cancel);
                Result<List<BarMapRotation>, string> parsed = _Parser.Parse(body);

                if (parsed.IsOk == false) {
                    _Logger.LogError($"failed to parse map rotation response [error={parsed.Error}]");
                    return parsed.Error;
                }

                _Logger.LogDebug($"loaded map rotations from file and stored in cache");

                _Cache.Set(CACHE_KEY, parsed.Value, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });

                rotations = parsed.Value;
            }

            return rotations;
        }

    }
}
