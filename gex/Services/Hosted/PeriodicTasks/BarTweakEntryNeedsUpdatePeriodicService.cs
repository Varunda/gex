using gex.Models.Bar;
using gex.Services.Db.Bar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class BarTweakEntryNeedsUpdatePeriodicService : AppBackgroundPeriodicService {

        private readonly BarTweakEntryNeedsUpdateDb _NeedsUpdateDb;
        private readonly BarTweakEntryDb _TweakEntryDb;

        public BarTweakEntryNeedsUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarTweakEntryNeedsUpdateDb needsUpdateDb,
            BarTweakEntryDb tweakEntryDb)
        : base("bar_tweak_entry_needs_update", TimeSpan.FromMinutes(1), loggerFactory, healthMon) {

            _NeedsUpdateDb = needsUpdateDb;
            _TweakEntryDb = tweakEntryDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"updating bar tweak entries");

            List<BarTweakEntryNeedsUpdate> pending = await _NeedsUpdateDb.GetReady(cancel);

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();
            int ok = 0;
            int err = 0;

            foreach (BarTweakEntryNeedsUpdate update in pending) {
                if (cancel.IsCancellationRequested) {
                    _Logger.LogInformation($"stop requested [ok={ok}] [err={err}]");
                    break;
                }

                stepTimer.Restart();
                _Logger.LogTrace($"updating tweaks for day [day={update.Day:u}]");
                try {
                    await _TweakEntryDb.Generate(update.Day, cancel);
                    long runMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
                    ++ok;
                    _Logger.LogDebug($"updated user units made [day={update.Day:u}] [timer={runMs}ms]");

                    await _NeedsUpdateDb.Remove(update, cancel);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to update tweaks entry for a day [day={update.Day:u}]");
                    ++err;
                }
            }

            _Logger.LogInformation($"updated tweaks [timer={timer.ElapsedMilliseconds}ms] [count={pending.Count}] [ok={ok}] [err={err}]");
            return $"updated tweaks [ok={ok}] [err={err}]";
        }

    }
}
