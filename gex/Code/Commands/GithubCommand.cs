using gex.Commands;
using gex.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class GithubCommand {

        private readonly ILogger<GithubCommand> _Logger;
        private readonly IGithubDownloadRepository _GithubDownloadRepository;

        public GithubCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<GithubCommand>>();
            _GithubDownloadRepository = services.GetRequiredService<IGithubDownloadRepository>();
        }

        public Task DownloadUnits() {
            Task.Run(async () => {
                _Logger.LogInformation($"downloading unit data");
                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(10));

                await _GithubDownloadRepository.DownloadFolder("units", true, cts.Token);
            });

            return Task.CompletedTask;
        }

        public Task DownloadI18n() {
            Task.Run(async () => {
                _Logger.LogInformation($"downloading i18n in languages/en");
                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(10));

                await _GithubDownloadRepository.DownloadFolder("language/en", true, cts.Token);
            });

            return Task.CompletedTask;
        }

        public Task DownloadWeapons() {
            Task.Run(async () => {
                _Logger.LogInformation($"downloading weapons");
                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(10));

                await _GithubDownloadRepository.DownloadFolder("weapons", true, cts.Token);
            });

            return Task.CompletedTask;
        }

    }
}
