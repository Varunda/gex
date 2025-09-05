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

    public class BarWeaponDefinitionRepository {

        private readonly ILogger<BarWeaponDefinitionRepository> _Logger;
        private readonly BarWeaponDefinitionParser _WeaponDefinitionParser;
        private readonly IGithubDownloadRepository _GithubRepository;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ALL = "Gex.WeaponDefinitions.All";

        public BarWeaponDefinitionRepository(ILogger<BarWeaponDefinitionRepository> logger,
            BarWeaponDefinitionParser weaponDefinitionParser, IGithubDownloadRepository githubRepository,
            IMemoryCache memoryCache) {

            _Logger = logger;
            _Cache = memoryCache;

            _WeaponDefinitionParser = weaponDefinitionParser;
            _GithubRepository = githubRepository;
        }

        private async Task<Result<List<BarWeaponDefinition>, string>> ParseAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out Result<List<BarWeaponDefinition>, string>? weaponDefs) == false || weaponDefs == null) {
                Result<List<string>, string> files = _GithubRepository.GetFiles("weapons");

                if (files.IsOk == false) {
                    weaponDefs = $"could not get files in weapons folder [error={files.Error}]";
                } else {
                    List<BarWeaponDefinition> defs = [];

                    foreach (string file in files.Value) {
                        if (file.EndsWith(".lua") == false) {
                            continue;
                        }

                        Result<string, string> fileData = await _GithubRepository.GetFile("weapons", file, cancel);
                        if (fileData.IsOk == false) {
                            weaponDefs = $"could not get weapon def file [error={fileData.Error}]";
                            break;
                        } else {
                            Result<List<BarWeaponDefinition>, string> fileWeaponDefs = await _WeaponDefinitionParser.Parse(fileData.Value, cancel);
                            if (fileWeaponDefs.IsOk == false) {
                                _Logger.LogWarning($"failed to parse weapon definition [file={file}] [error={fileWeaponDefs.Error}]");
                            } else {
                                defs.AddRange(fileWeaponDefs.Value);
                            }
                        }
                    }

                    weaponDefs = defs;
                }

                if (weaponDefs.IsOk == true) {
                    _Logger.LogInformation($"loaded weapon definitions [count={weaponDefs.Value.Count}]");
                } else {
                    _Logger.LogWarning($"failed to load weapon definitions [error={weaponDefs.Error}]");
                }

                _Cache.Set(CACHE_KEY_ALL, weaponDefs, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = weaponDefs.IsOk ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(1)
                });
            }

            return weaponDefs;
        }

        public async Task<Result<BarWeaponDefinition, string>> GetWeaponDefinition(string wepDefID, CancellationToken cancel) {
            Result<List<BarWeaponDefinition>, string> all = await ParseAll(cancel);
            if (all.IsOk == false) {
                return $"failed to get all weapon definitions [error={all.Error}]";
            }

            BarWeaponDefinition? def = all.Value.FirstOrDefault(iter => iter.DefinitionName.ToLower() == wepDefID.ToLower());
            if (def == null) {
                return $"failed to find weapon def '{wepDefID}'";
            }

            return def;
        }

    }
}
