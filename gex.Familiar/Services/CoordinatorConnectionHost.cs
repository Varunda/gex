using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Familiar.Services {

    public class CoordinatorConnectionHost : IHostedService {

        private readonly ILogger<CoordinatorConnectionHost> _Logger;
        private readonly FamiliarHubClient _Hub;

        public CoordinatorConnectionHost(ILogger<CoordinatorConnectionHost> logger,
            FamiliarHubClient hub) {

            _Logger = logger;
            _Hub = hub;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _Logger.LogInformation($"starting coordinator connection host");

            Task.Run(async () => {
                await Run(cancellationToken);
            }, cancellationToken);

            return Task.CompletedTask;
        }

        private async Task Run(CancellationToken cancel) {

            int errorCount = 0;

            while (cancel.IsCancellationRequested == false) {
                try {
                    if (_Hub.IsDisconnected() == true) {
                        _Logger.LogInformation($"starting connection");
                        await _Hub.Start(cancel);
                        _Logger.LogInformation($"connected!");
                    }

                    await Task.Delay(10, cancel);
                    errorCount = 0;
                } catch (Exception ex) {
                    int delaySec = Math.Min(60, Math.Max(1, errorCount++ + Random.Shared.Next(1, 5)));
                    _Logger.LogError(ex, $"error in Run of coordinator connection host [delay={delaySec}]");
                    await Task.Delay(TimeSpan.FromSeconds(delaySec), cancel);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
