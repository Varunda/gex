using gex.Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class SevenZipCheckStartupService : IHostedService {

        private readonly ILogger<SevenZipCheckStartupService> _Logger;
        private readonly PathEnvironmentService _PathUtil;

        public SevenZipCheckStartupService(ILogger<SevenZipCheckStartupService> logger,
            PathEnvironmentService pathUtil) {

            _Logger = logger;
            _PathUtil = pathUtil;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            string _ = _PathUtil.FindExecutable("7z") ?? throw new System.Exception($"failed to find 7z in PATH");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
