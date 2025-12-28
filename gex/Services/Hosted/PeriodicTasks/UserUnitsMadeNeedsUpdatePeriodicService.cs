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

    [Obsolete("kept as reference for when map opening labs update is done")]
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

            IEnumerable<IGrouping<DateTime, UserUnitsMadeNeedsUpdate>> updateDays = needsUpdate.GroupBy(iter => iter.Day);

            int dayCount = 0;
            int entryCount = 0;

            foreach (IGrouping<DateTime, UserUnitsMadeNeedsUpdate> day in updateDays) {
                ++dayCount;
                IEnumerable<long> userIDs = day.Select(iter => iter.UserID).Distinct();
                IEnumerable<string> mapFilenames = day.Select(iter => iter.MapFilename).Distinct();

                _Logger.LogDebug($"performing user units made update [day={day.Key:u}] [user id count={userIDs.Count()}] [map filenames count={mapFilenames.Count()}]");

                Stopwatch timer = Stopwatch.StartNew();
                List<BarUserUnitsMade> stats;
                try {
                    stats = await _UnitsMadeDb.Generate(userIDs, day.Key, mapFilenames, cancel);
                } catch (Exception ex) {
                    if (ex is NpgsqlException && ex.InnerException is TimeoutException) {
                        _Logger.LogWarning($"got timeout when generating user units made on day [day={day.Key:u}]");
                        continue;
                    }
                    throw;
                }
                long generateMs = timer.ElapsedMilliseconds; timer.Restart();

                foreach (BarUserUnitsMade made in stats) {
                    ++entryCount;
                    await _UnitsMadeDb.Upsert(made, cancel);
                }

                long upsertMs = timer.ElapsedMilliseconds; timer.Restart();

                foreach (UserUnitsMadeNeedsUpdate iter in day) {
                    await _UpdateDb.Remove(new UserUnitsMadeNeedsUpdate() {
                        Day = iter.Day,
                        UserID = iter.UserID,
                        MapFilename = iter.MapFilename,
                    }, cancel);
                }

                long removeMs = timer.ElapsedMilliseconds;

                _Logger.LogDebug($"generated user units made on day [day={day.Key:u}] [generate={generateMs}ms] [upsert={upsertMs}ms] [remove={removeMs}ms]");
            }

            _Logger.LogInformation($"finished user units made update [day count={dayCount}] [entry count={entryCount}]");
            return $"updated user units made [day count={dayCount}] [entry count={entryCount}]";
        }

    }
}
