using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class GitHubUnitDataUpdatePeriodicService : AppBackgroundPeriodicService {

        private const string SERVICE_NAME = "github_unit_data_update_service";
        private static readonly TimeSpan RUN_DELAY = TimeSpan.FromHours(4);

        private readonly IGithubDownloadRepository _GithubRepository;

        public GitHubUnitDataUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, IGithubDownloadRepository githubRepository)

        : base(SERVICE_NAME, RUN_DELAY, loggerFactory, healthMon) {

            _GithubRepository = githubRepository;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {
            _Logger.LogInformation($"performing GitHub data update");

            await _GithubRepository.DownloadFolder("units", false, true, cancel);
            await _GithubRepository.DownloadFolder("weapons", false, true, cancel);
            await _GithubRepository.DownloadFolder("language/en", false, true, cancel);
            await _GithubRepository.DownloadFolder("gamedata", false, true, cancel);
            await _GithubRepository.DownloadFolder("icons", false, false, cancel);

            return null;
        }

    }
}
