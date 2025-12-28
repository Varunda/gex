using gex.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarI18nRepository {

        private readonly ILogger<BarI18nRepository> _Logger;

        private readonly IGithubDownloadRepository _GithubRepository;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_DICT = "Gex.BarI18n.{0}"; // {0} => file

        public BarI18nRepository(ILogger<BarI18nRepository> logger,
            IGithubDownloadRepository githubRepository, IMemoryCache cache) {

            _Logger = logger;
            _GithubRepository = githubRepository;
            _Cache = cache;
        }

        private async Task<Dictionary<string, string>> GetAll(string file, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_DICT, file);

            if (_Cache.TryGetValue(cacheKey, out Dictionary<string, string>? dict) == false || dict == null) {

                if (_GithubRepository.HasFile("language/en", $"{file}.json") == false) {
                    _Logger.LogWarning($"missing file from Github data [file={file}]");
                    dict = new Dictionary<string, string>();
                } else {
                    Result<string, string> text = await _GithubRepository.GetFile("language/en", $"{file}.json", cancel);
                    if (text.IsOk == false) {
                        _Logger.LogWarning($"failed to get i18n contents of a file [file={file}] [error={text.Error}]");
                        dict = new Dictionary<string, string>();
                    } else {
                        JsonElement json = JsonSerializer.Deserialize<JsonElement>(text.Value);
                        dict = _MakeKeys(json);

                        _Cache.Set(cacheKey, dict, new MemoryCacheEntryOptions() {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                        });
                    }
                }
            }

            return dict;
        }

        public async Task<string?> GetString(string file, string key, CancellationToken cancel) {
            Dictionary<string, string> keys = await GetAll(file, cancel);
            return keys.GetValueOrDefault(key);
        }

        public Task<string?> GetUnitName(string defName, CancellationToken cancel) {
            return GetString("units", $"units.names.{defName}", cancel);
        }

        /// <summary>
        ///     get all i18n entries that start with <paramref name="prefix"/>, sorted by i18n key a->z
        /// </summary>
        /// <param name="file">file that contains the i18n data</param>
        /// <param name="prefix">prefix of the entries to include</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     an alphabetical (by <see cref="KeyValuePair{TKey, TValue}.Key"/>)
        ///     of all i18n entries that start with <paramref name="prefix"/> (case-sensitive)
        /// </returns>
        public async Task<List<KeyValuePair<string, string>>> GetKeysStartingWith(string file, string prefix, CancellationToken cancel) {
            Dictionary<string, string> keys = await GetAll(file, cancel);

            List<KeyValuePair<string, string>> match = [];
            foreach (KeyValuePair<string, string> iter in keys) {
                if (iter.Key.StartsWith(prefix)) {
                    match.Add(new KeyValuePair<string, string>(iter.Key, iter.Value));
                }
            }

            match = match.OrderBy(iter => iter.Key).ToList();

            return match;
        }

        private Dictionary<string, string> _MakeKeys(JsonElement json) {
            Dictionary<string, string> dict = [];

            foreach (JsonProperty elem in json.EnumerateObject()) {
                if (elem.Value.ValueKind == JsonValueKind.Object) {
                    Dictionary<string, string> child = _MakeKeys(elem.Value);
                    foreach (KeyValuePair<string, string> iter in child) {
                        dict.Add($"{elem.Name}.{iter.Key}", iter.Value);
                    }
                } else if (elem.Value.ValueKind == JsonValueKind.String) {
                    dict.Add($"{elem.Name}", elem.Value.GetString() ?? "");
                } else {
                    _Logger.LogWarning($"unhandled ValueKind of JsonProperty [name={elem.Name}]");
                }
            }

            return dict;
        }


    }
}
