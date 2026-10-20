using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Options;
using gex.Common.Services.Parser;
using gex.Services.Db.Map;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class MapParseCacheUpdatePeriodicService : AppBackgroundPeriodicService {

        private readonly BarMapParser _Parser;
        private readonly BarMapDataRepository _MapDataRepository;
        private readonly BarMapParseCacheDb _ParseCacheDb;
        private readonly IOptions<FileStorageOptions> _Options;

        private static bool _FirstRun = true;

        public MapParseCacheUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarMapParser parser,
            BarMapDataRepository mapDataRepository, IOptions<FileStorageOptions> options,
            BarMapParseCacheDb parseCacheDb)
        : base("map_parse_cache_update", TimeSpan.FromHours(4), loggerFactory, healthMon) {

            _Parser = parser;
            _MapDataRepository = mapDataRepository;
            _Options = options;
            _ParseCacheDb = parseCacheDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"performing map parse cache update");

            string mapsDir = Path.Join(_Options.Value.MapLocation, "maps");
            if (Directory.Exists(mapsDir) == false) {
                _Logger.LogError($"missing maps folder [mapsDir={mapsDir}]");
                return $"missing maps folder at '{mapsDir}'";
            }

            List<string> mapFilenames = _FirstRun == true ? await _ParseCacheDb.GetAllMapFilenames(cancel) : [];
            if (_FirstRun == true) {
                _Logger.LogDebug($"skipping maps already in cache for first run [maps={string.Join(", ", mapFilenames)}]");
            }

            int ok = 0;
            int err = 0;
            foreach (string mapDir in Directory.GetFiles(mapsDir)) {
                if (mapDir.EndsWith(".sd7") == false) {
                    continue;
                }

                if (_FirstRun == true) {
                    string mapFilename = Path.GetFileNameWithoutExtension(mapDir);
                    if (mapFilenames.Contains(mapFilename)) {
                        continue;
                    }
                }

                _Logger.LogInformation($"updating map parse cache for map [mapDir={mapDir}]");

                Result<BarMapData, string> result = await _Parser.Parse(mapDir, new BarMapParser.ParseOptions() {
                    HeightMap = true,
                    Header = true,
                    Smts = false,
                    Metal = true,
                    Features = true,
                }, cancel);

                if (result.IsOk == false) {
                    _Logger.LogError($"failed to parse map [map={mapDir}] [error={result.Error}]");
                    ++err;
                    continue;
                }

                _Logger.LogDebug($"upserting parsed map data [filename={result.Value.FileName}]");
                await _MapDataRepository.Upsert(result.Value.FileName, result.Value, cancel);
                ++ok;
            }

            _FirstRun = false;
            return $"updated map parse cache [ok={ok}] [error={err}]";
        }

    }
}
