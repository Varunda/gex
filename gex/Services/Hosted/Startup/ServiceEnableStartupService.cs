using gex.Models;
using gex.Models.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class ServiceEnableStartupService : IHostedService {

        private readonly ILogger<ServiceEnableStartupService> _Logger;
        private readonly IOptions<ServiceOptions> _Options;
        private readonly ServiceHealthMonitor _HealthMonitor;

        public ServiceEnableStartupService(ILogger<ServiceEnableStartupService> logger,
            IOptions<ServiceOptions> options, ServiceHealthMonitor healthMonitor) {

            _Logger = logger;
            _Options = options;
            _HealthMonitor = healthMonitor;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _Logger.LogInformation($"running startup service service");

            foreach (KeyValuePair<string, bool> iter in _Options.Value.Enabled) {
                _Logger.LogDebug($"setting service state [service={iter.Key}] [enabled={iter.Value}]");

                ServiceHealthEntry entry = _HealthMonitor.GetOrCreate(iter.Key);

                entry.Enabled = iter.Value;
                _HealthMonitor.Set(iter.Key, entry);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
