using gex.Models.MapStats;
using gex.Services.Db.MapStats;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class MapStatsNeedsUpdatePeriodicService : AppBackgroundPeriodicService {

        private readonly MapStatsNeedsUpdateDb _UpdateDb;
        private readonly MapStatsDailyOpeningLabDb _OpeningLabDb;
        private readonly MapStatsDailyUnitsMadeDb _UnitsMadeDb;

        public MapStatsNeedsUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, MapStatsNeedsUpdateDb updateDb,
            MapStatsDailyOpeningLabDb openingLabDb, MapStatsDailyUnitsMadeDb unitsMadeDb)
        : base("map_stats_needs_update", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

            _UpdateDb = updateDb;
            _OpeningLabDb = openingLabDb;
            _UnitsMadeDb = unitsMadeDb;
        }

        protected async override Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation("updating map stats that need an update");

            List<MapStatsNeedsUpdate> needsUpdate = await _UpdateDb.GetReady(cancel);
            _Logger.LogDebug($"found map stats that need an update [count={needsUpdate.Count}]");

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();
            int ok = 0;
            int err = 0;
            foreach (MapStatsNeedsUpdate update in needsUpdate) {
                stepTimer.Restart();
                _Logger.LogTrace($"updating opening lab stats for map [map={update.MapFilename}] [gamemode={update.Gamemode}] [day={update.Day:u}]");
                try {
                    await _OpeningLabDb.Generate(update, cancel);
                    long openingLabMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
                    await _UnitsMadeDb.Generate(update, cancel);
                    long unitsMadeMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
                    ++ok;
                    _Logger.LogDebug($"updated map stats [map={update.MapFilename}] [gamemode={update.Gamemode}] [day={update.Day:u}] "
                        + $"[opening lab={openingLabMs}ms] [units made={unitsMadeMs}ms]");

                    await _UpdateDb.Remove(update, cancel);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to update map stats [map={update.MapFilename}] [gamemode={update.Gamemode}] [day={update.Day:u}]");
                    ++err;
                }

            }

            _Logger.LogInformation($"updated map stats [timer={timer.ElapsedMilliseconds}ms] [ok={ok}] [err={err}]");
            return $"updated map stats [ok={ok}] [err={err}]";
        }

    }
}
