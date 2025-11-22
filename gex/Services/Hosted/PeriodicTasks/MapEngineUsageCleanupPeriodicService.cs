using gex.Common.Code.ExtensionMethods;
using gex.Common.Services;
using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class MapEngineUsageCleanupPeriodicService : AppBackgroundPeriodicService {

        private readonly MapEngineUsageDb _MapEngineUsageDb;
        private readonly EnginePathUtil _EnginePathUtil;

        public MapEngineUsageCleanupPeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, MapEngineUsageDb mapEngineUsageDb,
            EnginePathUtil enginePathUtil)
        : base("map_engine_usage_cleanup", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

            _MapEngineUsageDb = mapEngineUsageDb;
            _EnginePathUtil = enginePathUtil;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"performing map clean up");

            List<MapEngineUsage> expired = await _MapEngineUsageDb.GetExpired(cancel);
            _Logger.LogInformation($"got expired map usages [count={expired.Count}]");

            foreach (MapEngineUsage usage in expired) {
                string mapName = (usage.Map + ".sd7").EscapeRecoilFilesytemCharacters().ToLower();
                string mapPath = Path.Join(_EnginePathUtil.Get(usage.Engine), "maps", "maps", mapName);

                usage.DeletedOn = DateTime.UtcNow;
                
                if (File.Exists(mapPath) == false) {
                    _Logger.LogInformation($"map already deleted from engine [engine={usage.Engine}] [map={usage.Map}] [path={mapPath}]");
                    await _MapEngineUsageDb.MarkDeleted(usage, cancel);
                    continue;
                }

                _Logger.LogInformation($"deleting map from engine [engine={usage.Engine}] [map={usage.Map}] [path={mapPath}]");
                try {
                    File.Delete(mapPath);
                    await _MapEngineUsageDb.MarkDeleted(usage, cancel);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to delete map in engine [engine={usage.Engine}] [map={usage.Map}] [path={mapPath}]");
                }
            }

            return $"deleted {expired.Count} maps";
        }

    }
}
