using gex.Common.Models;
using gex.Models.Bar;
using gex.Services.Parser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMoveDefinitionRepository {

        private readonly ILogger<BarMoveDefinitionRepository> _Logger;
        private readonly BarMoveDefinitionParser _Parser;
        private readonly IGithubDownloadRepository _GithubDownloader;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_ALL = "Gex.MoveDefinitions.All";

        public BarMoveDefinitionRepository(ILogger<BarMoveDefinitionRepository> logger,
            BarMoveDefinitionParser parser, IGithubDownloadRepository githubDownloader,
            IMemoryCache cache) {

            _Logger = logger;
            _Parser = parser;
            _GithubDownloader = githubDownloader;
            _Cache = cache;
        }

        public async Task<Result<Dictionary<string, BarMoveDefinition>, string>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out Dictionary<string, BarMoveDefinition>? dict) == true && dict != null) {
                return dict;
            }

            _Logger.LogInformation("loading move definitions from movedefs.lua");

            Result<string, string> file = await _GithubDownloader.GetFile("gamedata", "movedefs.lua", cancel);
            if (file.IsOk == false) {
                return file.Error;
            }

            Result<Dictionary<string, BarMoveDefinition>, string> result = await _Parser.GetAll(file.Value, cancel);
            if (result.IsOk == true) {
                _Cache.Set(CACHE_KEY_ALL, result.Value, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                });
            }

            return result;
        }

        public async Task<Result<BarMoveDefinition?, string>> Get(string name, CancellationToken cancel) {
            Result<Dictionary<string, BarMoveDefinition>, string> allDefs = await GetAll(cancel);
            if (allDefs.IsOk == false) {
                return allDefs.Error;
            }

            BarMoveDefinition? def = allDefs.Value.GetValueOrDefault(name);
            if (def == null) {
                return $"failed to find move definition '{name}'";
            }

            return def;
        }

    }
}
