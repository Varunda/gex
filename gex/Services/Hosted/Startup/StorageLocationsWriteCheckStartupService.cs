using gex.Models.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class StorageLocationsWriteCheckStartupService : IHostedService {

        private readonly ILogger<StorageLocationsWriteCheckStartupService> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        public StorageLocationsWriteCheckStartupService(ILogger<StorageLocationsWriteCheckStartupService> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        public async Task StartAsync(CancellationToken cancel) {
            _Logger.LogInformation($"ensuring process can write to locations");

            await _TestFile(_Options.Value.ReplayLocation, cancel);
            await _TestFile(_Options.Value.TempWorkLocation, cancel);
            await _TestFile(_Options.Value.EngineLocation, cancel);
            await _TestFile(_Options.Value.GameLogLocation, cancel);

            _Logger.LogInformation($"write test complete!");
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        private async Task _TestFile(string path, CancellationToken cancel) {
            _Logger.LogDebug($"performing test write [path={path}]");

            if (string.IsNullOrEmpty(path)) {
                throw new Exception($"missing path!");
            }

            string filePath = path + Path.DirectorySeparatorChar + "test_write.txt";

            using (FileStream testFile = File.OpenWrite(filePath)) {
                await testFile.WriteAsync(Encoding.UTF8.GetBytes("hello!"), cancel);
            }

            File.Delete(filePath);
        }

    }
}
