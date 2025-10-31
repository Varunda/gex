using gex.Common.Models;
using gex.Common.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Storage {

    public class DemofileStorage {

        private readonly ILogger<DemofileStorage> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        public DemofileStorage(ILogger<DemofileStorage> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        public async Task<Result<byte[], string>> GetDemofileByFilename(string filename, CancellationToken cancel) {
            string replayPath = Path.Join(_Options.Value.ReplayLocation, filename);
            if (File.Exists(replayPath) == false) {
                return $"missing demofile at '{replayPath}";
            }

            FileInfo fi = new(replayPath);
            if (fi.Length > 1024 * 1024 * 64) {
                _Logger.LogWarning($"demo file is much larger than expected, refusing to parse this [size={fi.Length}] [path={replayPath}]");
                return $"demofile too large";
            }

            byte[] file = await File.ReadAllBytesAsync(replayPath, cancel);
            return file;
        }

    }
}
