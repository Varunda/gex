using gex.Services.Db.UserStats;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class UserSkillPercentileUpdatePeriodicServer : AppBackgroundPeriodicService {

        private readonly BarUserSkillPercentileDb _PercentileDb;

        public UserSkillPercentileUpdatePeriodicServer(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarUserSkillPercentileDb percentileDb)
            : base("bar_user_skill_percentile_update", TimeSpan.FromMinutes(30), loggerFactory, healthMon
        ) {

            _PercentileDb = percentileDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogDebug($"updating bar_user_skill_percentile");
            await _PercentileDb.Generate(cancel);
            _Logger.LogInformation($"updated bar_user_skill_percentil");

            return "updating bar_user_skill_percentile";
        }

    }
}
