using gex.Models.Db;
using gex.Services.Db.Match;
using gex.Services.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class ActionLogDeleterPeriodicService : AppBackgroundPeriodicService {

        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly GameOutputStorage _OutputStorage;

        private readonly static TimeSpan CUT_OFF = TimeSpan.FromDays(90);

        public ActionLogDeleterPeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarMatchProcessingDb processingDb,
            GameOutputStorage outputStorage)
        : base("action_log_deleter", TimeSpan.FromMinutes(15), loggerFactory, healthMon) {

            _ProcessingDb = processingDb;
            _OutputStorage = outputStorage;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"deleting old action logs [cutoff={CUT_OFF}]");

            int count = 0;
            List<BarMatchProcessing> old = [];
            do {
                old = await _ProcessingDb.NeedsActionLogDeleted(CUT_OFF, cancel);

                _Logger.LogInformation($"found games to delete action log of [count={old.Count}]");

                foreach (BarMatchProcessing p in old) {
                    _Logger.LogDebug($"deleting old action log of game [gameID={p.GameID}]");

                    try {
                        _OutputStorage.DeleteActionLog(p.GameID);
                        p.ActionsDeleted = DateTime.UtcNow;
                        await _ProcessingDb.Upsert(p);
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to delete action log [gameID={p.GameID}]");
                    }
                    ++count;
                }
            } while (old.Count > 0);

            _Logger.LogInformation($"deleted old action logs [count={count}]");
            return $"deleted {count} old action logs";
        }

    }
}
