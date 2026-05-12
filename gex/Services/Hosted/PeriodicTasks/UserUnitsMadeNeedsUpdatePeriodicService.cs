using gex.Models.Db;
using gex.Models.UserStats;
using gex.Services.Db;
using gex.Services.Db.UserStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class UserUnitsMadeNeedsUpdatePeriodicService : AppBackgroundPeriodicService {

        private readonly UserUnitsMadeNeedsUpdateDb _UpdateDb;
        private readonly BarUserUnitsMadeDb _UnitsMadeDb;

        public UserUnitsMadeNeedsUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, UserUnitsMadeNeedsUpdateDb updateDb,
            BarUserUnitsMadeDb unitsMadeDb)
        : base("user_units_made_update", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

            _UpdateDb = updateDb;
            _UnitsMadeDb = unitsMadeDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"performing user units made update");

            List<UserUnitsMadeNeedsUpdate> needsUpdate = await _UpdateDb.GetReady(cancel);
            _Logger.LogDebug($"loaded needs update for user units made [count={needsUpdate.Count}]");

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();
            int ok = 0;
            int err = 0;
            foreach (UserUnitsMadeNeedsUpdate update in needsUpdate) {
                if (cancel.IsCancellationRequested) {
                    _Logger.LogInformation($"stop requested [ok={ok}] [err={err}]");
                    break;
                }

                stepTimer.Restart();
                _Logger.LogTrace($"updating user units made [userID={update.UserID}] [map={update.MapFilename}] [gamemode={update.Gamemode}] [day={update.Day:u}]");
                try {
                    await _UnitsMadeDb.Generate(update, cancel);
                    long unitsMadeMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
                    ++ok;
                    _Logger.LogDebug($"updated user units made [userID={update.UserID}] [map={update.MapFilename}] [gamemode={update.Gamemode}] [day={update.Day:u}] "
                        + $"[units made={unitsMadeMs}ms]");

                    await _UpdateDb.Remove(update, cancel);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to update user units made "
                        + $"[userID={update.UserID}] [map={update.MapFilename}] [gamemode={update.Gamemode}] [day={update.Day:u}]");
                    ++err;
                }

            }

            _Logger.LogInformation($"updated user units made [timer={timer.ElapsedMilliseconds}ms] [ok={ok}] [err={err}]");
            return $"updated user units made [ok={ok}] [err={err}]";
        }

    }
}
