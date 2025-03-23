using gex.Models.Db;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class GameVersionCleanupPeriodicService : AppBackgroundPeriodicService {

        private readonly BarMatchRepository _MatchRepository;

        public GameVersionCleanupPeriodicService(ILoggerFactory loggerFactory, ServiceHealthMonitor healthMon,
            BarMatchRepository matchRepository)
        : base("game_version_cleanup", TimeSpan.FromMinutes(5), loggerFactory, healthMon) {

            _MatchRepository = matchRepository;

        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {

            List<BarMatch> match = await _MatchRepository.GetByTimePeriod(DateTime.UtcNow - TimeSpan.FromDays(1), DateTime.UtcNow, cancel);

            HashSet<string> gameVersion = new HashSet<string>(match.Select(iter => iter.GameVersion).Distinct());

            return null;
        }

    }
}
