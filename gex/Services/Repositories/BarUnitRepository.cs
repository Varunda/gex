using gex.Models;
using gex.Models.Bar;
using gex.Services.Parser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarUnitRepository {

        private readonly ILogger<BarUnitRepository> _Logger;

        private readonly BarUnitParser _UnitParser;
        private readonly IGithubDownloadRepository _GithubRepository;
        private readonly BarI18nRepository _I18n;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ALL = "Gex.UnitData.All";
        private const string CACHE_KEY = "Gex.UnitData.Unit.{0}"; // {0} => definition name

        public BarUnitRepository(ILogger<BarUnitRepository> logger,
            BarUnitParser unitParser, IGithubDownloadRepository githubRepository,
            IMemoryCache cache, BarI18nRepository i18n) {

            _Logger = logger;
            _UnitParser = unitParser;
            _GithubRepository = githubRepository;
            _Cache = cache;
            _I18n = i18n;
        }

        /// <summary>
        ///     check if a unit definition file exists
        /// </summary>
        /// <param name="defName">name of the unit definition. case-sensitive</param>
        /// <returns></returns>
        public bool HasUnit(string defName) {
            return _GithubRepository.HasFile("units", $"{defName}.lua");
        }

        /// <summary>
        ///     get a list of all unit names as definition name and display name pairs
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<BarUnitName>> GetAllNames(CancellationToken cancel) {

            if (_Cache.TryGetValue(CACHE_KEY_ALL, out List<BarUnitName>? unitNames) == true && unitNames != null) {
                return unitNames;
            }

            List<KeyValuePair<string, string>> i18nNames = await _I18n.GetKeysStartingWith("units", "units.names.", cancel);
            if (i18nNames.Count == 0) {
                throw new Exception($"no units found?");
            }

            // create a list of unique names to all of the definition names that use that name
            // <display name, unit defs[]>
            Dictionary<string, List<string>> nameSets = [];
            foreach (KeyValuePair<string, string> iter in i18nNames) {
                List<string> defNames = nameSets.GetValueOrDefault(iter.Value) ?? new List<string>();
                defNames.Add(iter.Key["units.names.".Length..]); // remove the prefix units.names. from all entries
                nameSets[iter.Value] = defNames;
            }

            List<BarUnitName> names = [];

            foreach (KeyValuePair<string, List<string>> set in nameSets) {
                if (set.Value.Count > 1) {

                    // if the definition names change in just faction prefix (e.g. armalab, coralab)
                    // then gex can safely just suffix the label name with the faction,
                    // else just include the full definition name
                    bool justFactionPrefixChanges = set.Value.Select(iter => {
                        if (iter.StartsWith("cor") || iter.StartsWith("arm") || iter.StartsWith("leg")) {
                            return iter[3..];
                        }
                        return iter;
                    }).Distinct().Count() == 1;

                    if (justFactionPrefixChanges == true) {
                        foreach (string defName in set.Value) {
                            if (HasUnit(defName) == false) {
                                continue;
                            }

                            string labelName = set.Key + " ";
                            if (defName.StartsWith("cor")) {
                                labelName += "(Cortex)";
                            } else if (defName.StartsWith("arm")) {
                                labelName += "(Armada)";
                            } else if (defName.StartsWith("leg")) {
                                labelName += "(Legion)";
                            } else {
                                labelName += $"({defName})";
                            }

                            names.Add(new BarUnitName() { DefinitionName = defName, DisplayName = labelName });
                        }
                    } else {
                        foreach (string defName in set.Value) {
                            if (HasUnit(defName) == false) {
                                _Logger.LogDebug($"missing unit from units folder [defName={defName}]");
                                continue;
                            }

                            names.Add(new BarUnitName() { DefinitionName = defName, DisplayName = $"{set.Key} ({defName})" });
                        }
                    }
                } else {
                    if (HasUnit(set.Value[0]) == false) {
                        _Logger.LogDebug($"missing unit from units folder [defName={set.Value[0]}]");
                        continue;
                    }
                    names.Add(new BarUnitName() { DefinitionName = set.Value[0], DisplayName = set.Key });
                }
            }

            _Cache.Set(CACHE_KEY_ALL, names, new MemoryCacheEntryOptions() {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return names;
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
