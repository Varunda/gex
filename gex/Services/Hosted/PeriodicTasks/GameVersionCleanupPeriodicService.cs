using gex.Models.Db;
using gex.Models.Options;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

	/// <summary>
	///		periodic task that deletes old game versions that aren't used anymore
	/// </summary>
    public class GameVersionCleanupPeriodicService : AppBackgroundPeriodicService {

        private readonly GameVersionUsageDb _GameVersionUsageDb;
		private readonly EnginePathUtil _EnginePathUtil;

		public GameVersionCleanupPeriodicService(ILoggerFactory loggerFactory, ServiceHealthMonitor healthMon,
			GameVersionUsageDb gameVersionUsageDb, EnginePathUtil enginePathUtil)
		: base("game_version_cleanup", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

			_GameVersionUsageDb = gameVersionUsageDb;
			_EnginePathUtil = enginePathUtil;
		}

		protected override async Task<string?> PerformTask(CancellationToken cancel) {

			_Logger.LogInformation($"deleting old game versions no longer in use");

            List<GameVersionUsage> expired = await _GameVersionUsageDb.GetExpired(cancel);
			_Logger.LogDebug($"found game versions to delete [count={expired.Count}]");

			foreach (GameVersionUsage entry in expired) {
				string enginePath = Path.Join(_EnginePathUtil.Get(entry.Engine), "games", entry.Version);

				if (Directory.Exists(enginePath) == false) {
					_Logger.LogInformation($"game version already deleted [engine={entry.Engine}] [version={entry.Version}] [path={enginePath}]");
					await _GameVersionUsageDb.MarkDeleted(entry.Engine, entry.Version, DateTime.UtcNow, cancel);
					continue;
				}

				_Logger.LogDebug($"deleting expired version [engine={entry.Engine}] [version={entry.Version}] [path={enginePath}]");
				try {
					Directory.Delete(enginePath, true);
					await _GameVersionUsageDb.MarkDeleted(entry.Engine, entry.Version, DateTime.UtcNow, cancel);
                    _Logger.LogInformation($"deleted expired version [engine={entry.Engine}] [version={entry.Version}] [path={enginePath}]");
				} catch (Exception ex) {
					_Logger.LogError(ex, $"failed to delete un-used game version [engine={entry.Engine}] [version={entry.Version}] [path={enginePath}]");
				}
			}

            return null;
        }

    }
}
