using gex.Models;
using gex.Models.Bar;
using gex.Services.Parser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarUnitRepository {

        private readonly ILogger<BarUnitRepository> _Logger;

        private readonly BarUnitParser _UnitParser;
        private readonly IGithubDownloadRepository _GithubRepository;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY = "Gex.UnitData.{0}"; // {0} => definition name

        public BarUnitRepository(ILogger<BarUnitRepository> logger,
            BarUnitParser unitParser, IGithubDownloadRepository githubRepository,
            IMemoryCache cache) {

            _Logger = logger;
            _UnitParser = unitParser;
            _GithubRepository = githubRepository;
            _Cache = cache;
        }

        public bool HasUnit(string defName) {
            return _GithubRepository.HasFile("units", $"{defName}.lua");
        }

        public async Task<Result<BarUnit, string>> GetByDefinitionName(string defName, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, defName);

            if (_Cache.TryGetValue(cacheKey, out Result<BarUnit, string>? result) == false || result == null) {
                Result<string, string> contents = await _GithubRepository.GetFile("units", $"{defName}.lua", cancel);
                if (contents.IsOk == false) {
                    result = $"error getting unit file [error={contents.Error}]";
                } else {
                    Result<BarUnit, string> parsed = await _UnitParser.Parse(contents.Value, cancel);
                    result = parsed;
                }

                _Cache.Set(cacheKey, result, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = result.IsOk == true ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(1)
                });
            }

            return result;
        }

    }
}
