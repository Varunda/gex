using gex.Models.Queues;
using gex.Services.Db.MapStats;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class MapStatUpdateQueueProcessor : BaseQueueProcessor<MapStatUpdateQueueEntry> {

        private readonly MapStatsDb _MapStatsDb;
        private readonly MapStatsStartSpotDb _StartSpotDb;
        private readonly MapStatsByFactionDb _FactionStatsDb;
        private readonly MapStatsOpeningLabDb _OpeningLabDb;

        /// <summary>
        ///		timeout for map updates, don't allow maps to update quicker than once every 5 minutes
        /// </summary>
        private static readonly Dictionary<string, DateTime> _LastUpdated = new();

        private static readonly TimeSpan _MapUpdateMinimumSpan = TimeSpan.FromMinutes(5);

        public MapStatUpdateQueueProcessor(ILoggerFactory factory,
            BaseQueue<MapStatUpdateQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            MapStatsDb mapStatsDb, MapStatsStartSpotDb startSpotDb, MapStatsByFactionDb factionStatsDb,
            MapStatsOpeningLabDb openingLabDb)
        : base("map_stat_update_queue", factory, queue, serviceHealthMonitor) {

            _MapStatsDb = mapStatsDb;
            _StartSpotDb = startSpotDb;
            _FactionStatsDb = factionStatsDb;
            _OpeningLabDb = openingLabDb;
        }

        protected override async Task<bool> _ProcessQueueEntry(MapStatUpdateQueueEntry entry, CancellationToken cancel) {

            if (_LastUpdated.ContainsKey(entry.MapFilename) == true) {
                TimeSpan diff = DateTime.UtcNow - _LastUpdated.GetValueOrDefault(entry.MapFilename);
                if (diff <= _MapUpdateMinimumSpan) {
                    _Logger.LogDebug($"map was recently updated, not updating again [mapFilename={entry.MapFilename}] [diff={diff}]");
                    return false;
                }
            }

            _Logger.LogDebug($"updating map stats [mapFilename={entry.MapFilename}]");

            Stopwatch overAllTimer = Stopwatch.StartNew();
            Stopwatch timer = Stopwatch.StartNew();
            await _MapStatsDb.Generate(entry.MapFilename, cancel);
            long statsMs = timer.ElapsedMilliseconds; timer.Restart();

            await _StartSpotDb.Generate(entry.MapFilename, cancel);
            long startMs = timer.ElapsedMilliseconds; timer.Restart();

            await _FactionStatsDb.Generate(entry.MapFilename, cancel);
            long factionMs = timer.ElapsedMilliseconds; timer.Restart();

            await _OpeningLabDb.Generate(entry.MapFilename, cancel);
            long openingLabMs = timer.ElapsedMilliseconds; timer.Restart();

            _Logger.LogInformation($"updated map stats [mapFilename={entry.MapFilename}] [timer={overAllTimer.ElapsedMilliseconds}ms] "
                + $"[stats={statsMs}ms] [start spots={startMs}ms] [faction={factionMs}ms] [opening lab={openingLabMs}ms]");

            _LastUpdated[entry.MapFilename] = DateTime.UtcNow;

            return true;
        }

    }
}
