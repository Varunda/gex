using gex.Common.Models;
using gex.Services.Parser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarIconTypeRepository {

        private static readonly HttpClient _Http = new HttpClient();

        static BarIconTypeRepository() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex/0.1 (discord: varunda)");
        }

        private readonly ILogger<BarIconTypeRepository> _Logger;
        private readonly BarIconTypeParser _Parser;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY = "Gex.IconType.All";

        public BarIconTypeRepository(ILogger<BarIconTypeRepository> logger,
            IMemoryCache cache, BarIconTypeParser parser) {

            _Logger = logger;
            _Cache = cache;
            _Parser = parser;
        }

        public async Task<Result<Dictionary<string, string>, string>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY, out Dictionary<string, string>? result) == true && result != null) {
                return result;
            }

            string url = "https://raw.githubusercontent.com/beyond-all-reason/Beyond-All-Reason/refs/heads/master/gamedata/icontypes.lua";

            HttpResponseMessage response = await _Http.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK) {
                _Logger.LogError($"failed to load icontypes [statusCode={response.StatusCode}] [response={body.Take(200)}]");
                return $"got non-200OK response: {response.StatusCode}";
            }

            Result<Dictionary<string, string>, string> dict = await _Parser.Parse(body, cancel);
            if (dict.IsOk == false) {
                return $"error parsing icon types: {dict.Error}";
            }

            _Cache.Set(CACHE_KEY, dict.Value, new MemoryCacheEntryOptions() {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
            });

            return dict.Value;
        }

        public async Task<Result<string?, string>> GetUnitDefIcon(string unitDef, CancellationToken cancel) {
            Result<Dictionary<string, string>, string> dict = await GetAll(cancel);
            if (dict.IsOk == false) {
                return Result<string?, string>.Err(dict.Error);
            }

            return Result<string?, string>.Ok(dict.Value.GetValueOrDefault(unitDef));
        }

    }
}
