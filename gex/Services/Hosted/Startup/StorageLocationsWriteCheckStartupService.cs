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

    /// <summary>
    ///		startup check to ensure that all storage locations are set and writable
    /// </summary>
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

            bool error = false;

            if (string.IsNullOrEmpty(_Options.Value.ReplayLocation)) {
                _Logger.LogError("the option 'ReplayLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (string.IsNullOrEmpty(_Options.Value.TempWorkLocation)) {
                _Logger.LogError("the option 'TempWorkLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (string.IsNullOrEmpty(_Options.Value.EngineLocation)) {
                _Logger.LogError("the option 'EngineLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (string.IsNullOrEmpty(_Options.Value.GameLogLocation)) {
                _Logger.LogError("the option 'GameLogLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (string.IsNullOrEmpty(_Options.Value.WebImageLocation)) {
                _Logger.LogError("the option 'WebImageLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (string.IsNullOrEmpty(_Options.Value.GitHubDataLocation)) {
                _Logger.LogError("the option 'GitHubDataLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (string.IsNullOrEmpty(_Options.Value.UnitPositionLocation)) {
                _Logger.LogError("the option 'UnitPositionLocation' was an empty string or unset. Is this set in env.json?");
                error = true;
            }

            if (error == true) {
                throw new Exception($"one or more storage locations were not set. logs above will contain more info");
            }

            await _TestFile(_Options.Value.ReplayLocation, cancel);
            await _TestFile(_Options.Value.TempWorkLocation, cancel);
            await _TestFile(_Options.Value.EngineLocation, cancel);
            await _TestFile(_Options.Value.GameLogLocation, cancel);
            await _TestFile(_Options.Value.WebImageLocation, cancel);
            await _TestFile(_Options.Value.GitHubDataLocation, cancel);

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

            // this is not collapsed, as the stream would need to be closed in order for File.Delete to work
            using (FileStream testFile = File.OpenWrite(filePath)) {
                await testFile.WriteAsync(Encoding.UTF8.GetBytes("hello!"), cancel);
            }

            File.Delete(filePath);
        }

    }
}
