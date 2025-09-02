using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class GitHubUnitDataUpdatePeriodicService : AppBackgroundPeriodicService {

        private const string SERVICE_NAME = "github_unit_data_update_service";
        private static readonly TimeSpan RUN_DELAY = TimeSpan.FromHours(4);

        private readonly BarUnitGithubRepository _UnitGithubRepository;

        public GitHubUnitDataUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarUnitGithubRepository unitGithubRepository)

        : base(SERVICE_NAME, RUN_DELAY, loggerFactory, healthMon) {

            _UnitGithubRepository = unitGithubRepository;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"performing GitHub unit data update");

            await _UnitGithubRepository.DownloadAll(cancel);

            return null;
        }

    }
}
