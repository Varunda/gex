using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class ExampleStartupService : IHostedService {

        private readonly ILogger<ExampleStartupService> _Logger;

        public ExampleStartupService(ILogger<ExampleStartupService> logger
            ) {

            _Logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _Logger.LogInformation($"startup service complete!");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
