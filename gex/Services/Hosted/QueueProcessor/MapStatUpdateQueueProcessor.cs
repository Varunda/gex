using gex.Models.Queues;
using gex.Services.Db.MapStats;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

	public class MapStatUpdateQueueProcessor : BaseQueueProcessor<MapStatUpdateQueueEntry> {

		private readonly MapStatsDb _MapStatsDb;

		public MapStatUpdateQueueProcessor(ILoggerFactory factory,
			BaseQueue<MapStatUpdateQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
			MapStatsDb mapStatsDb)
		: base("map_stat_update_queue", factory, queue, serviceHealthMonitor) {

			_MapStatsDb = mapStatsDb;
		}

		protected override async Task<bool> _ProcessQueueEntry(MapStatUpdateQueueEntry entry, CancellationToken cancel) {
			_Logger.LogDebug($"updating map stats [mapFilename={entry.MapFilename}]");
			await _MapStatsDb.Generate(entry.MapFilename, cancel);
			return true;
		}

	}
}
