using gex.Models;
using gex.Models.Options;
using gex.Services.BarApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarReplayFileRepository {

        private readonly ILogger<BarReplayFileRepository> _Logger;
        private readonly BarReplayFileApi _Api;
        private readonly IOptions<FileStorageOptions> _Options;

        public BarReplayFileRepository(ILogger<BarReplayFileRepository> logger,
            BarReplayFileApi api, IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Api = api;
            _Options = options;
        }

        public async Task<Result<byte[], string>> GetReplay(string fileName, CancellationToken cancel = default) {
            string filePath = _Options.Value.ReplayLocation + Path.DirectorySeparatorChar + fileName;

            _Logger.LogDebug($"checking for already downloaded replay file [fileName={fileName}] [filePath={filePath}]");
            if (File.Exists(filePath)) {
                return await File.ReadAllBytesAsync(filePath, cancel);
            }

            _Logger.LogDebug($"loading replay file from api [filename={fileName}]");
            Result<byte[], string> result = await _Api.DownloadReplay(fileName, cancel);

            if (result.IsOk == false) {
                return result.Error;
            }

            using FileStream file = File.OpenWrite(filePath);
            await file.WriteAsync(result.Value, cancel);

            return result.Value;
        }

    }
}
