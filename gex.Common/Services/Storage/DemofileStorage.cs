using gex.Common.Models;
using gex.Common.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gex.Common.Services.Storage {

    public class DemofileStorage {

        private readonly ILogger<DemofileStorage> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        public DemofileStorage(ILogger<DemofileStorage> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        ///     check if a demofile has already been stored
        /// </summary>
        /// <param name="filename">filename of the demofile</param>
        public bool HasDemofile(string filename) {
            string replayPath = Path.Join(_Options.Value.ReplayLocation, filename);
            return File.Exists(replayPath);
        }

        /// <summary>
        ///     get the data of a demofile as a byte array
        /// </summary>
        /// <param name="filename">name of the demofile</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<byte[], string>> GetDemofileByFilename(string filename, CancellationToken cancel) {
            string replayPath = Path.Join(_Options.Value.ReplayLocation, filename);
            if (File.Exists(replayPath) == false) {
                return $"missing demofile at '{replayPath}'";
            }

            FileInfo fi = new(replayPath);
            if (fi.Length > 1024 * 1024 * 64) {
                _Logger.LogWarning($"demo file is much larger than expected, refusing to parse this [size={fi.Length}] [path={replayPath}]");
                return $"demofile too large";
            }

            byte[] file = await File.ReadAllBytesAsync(replayPath, cancel);
            return file;
        }

        /// <summary>
        ///     write a demofile to storage
        /// </summary>
        /// <param name="filename">name of the demofile</param>
        /// <param name="data">data of the demofile</param>
        /// <param name="override">if the file already exists, will it be replaced with the data in <paramref name="data"/>?</param>
        /// <param name="cancel">cancellation token</param>
        public async Task Write(string filename, byte[] data, bool @override, CancellationToken cancel) {
            string outputPath = Path.Join(_Options.Value.ReplayLocation, filename);

            if (File.Exists(outputPath) && @override == false) {
                _Logger.LogDebug($"demofile already exists [filename={filename}] [path={outputPath}]");
                return;
            }

            if (File.Exists(outputPath) == true) {
                File.Delete(outputPath);
            }

            using FileStream file = File.OpenWrite(outputPath);
            await file.WriteAsync(data, cancel);
            _Logger.LogInformation($"successfully saved demofile [filename={filename}] [outputPath={outputPath}]");
        }

    }
}
