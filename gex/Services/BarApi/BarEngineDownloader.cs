using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarEngineDownloader {

        private readonly ILogger<BarEngineDownloader> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly PathEnvironmentService _PathUtil;

        private static readonly HttpClient _Http = new HttpClient();

        private const string BASE_URL = "https://github.com/beyond-all-reason/spring/releases/download";

        private const string VERSION_PATH = "{0}/spring_bar_.rel2501.{0}_{1}-64-minimal-portable.7z"; // {0} => version, {1} => windows/linux

        static BarEngineDownloader() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

        public BarEngineDownloader(ILogger<BarEngineDownloader> logger,
            IOptions<FileStorageOptions> options, PathEnvironmentService pathUtil) {

            _Logger = logger;
            _Options = options;
            _PathUtil = pathUtil;
        }

        public bool HasEngine(string version) {
            string path = _Options.Value.EngineLocation + Path.DirectorySeparatorChar + version;
            return Directory.Exists(path);
        }

        /// <summary>
        ///     download a specific engine version
        /// </summary>
        /// <param name="version">name of the version to download</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public async Task DownloadEngine(string version, CancellationToken cancel) {
            string path = _Options.Value.EngineLocation + Path.DirectorySeparatorChar + version;

            if (Directory.Exists(path)) {
                _Logger.LogInformation($"engine version already downloaded [version={version}]");
                return;
            }

            Directory.CreateDirectory(path);

            Stopwatch timer = Stopwatch.StartNew();
            string versionPath = string.Format(VERSION_PATH, version, OperatingSystem.IsWindows() ? "windows" : "linux");
            _Logger.LogTrace($"getting version [versionPath={versionPath}]");
            HttpResponseMessage response = await _Http.GetAsync(BASE_URL + "/" + versionPath);

            if (response.IsSuccessStatusCode == false) {
                _Logger.LogError($"failed to download engine [version={version}] [status code={response.StatusCode}]");
                return;
            }

            string outputPath = path + Path.DirectorySeparatorChar + "engine.7z";
            using (FileStream output = File.OpenWrite(outputPath)) {
                await response.Content.CopyToAsync(output, cancel);
            }

            string sevenzipApp = _PathUtil.FindExecutable("7z") ?? throw new System.Exception($"failed to find 7z in PATH");

            using Process sevenZipProc = new();
            sevenZipProc.StartInfo.FileName = sevenzipApp;
            sevenZipProc.StartInfo.WorkingDirectory = path;
            sevenZipProc.StartInfo.Arguments = $"x -y engine.7z"; // -y assumes yes
            sevenZipProc.StartInfo.UseShellExecute = false;
            sevenZipProc.StartInfo.RedirectStandardOutput = true;
            sevenZipProc.Start();

            string stdout = await sevenZipProc.StandardOutput.ReadToEndAsync(cancel);
            await sevenZipProc.WaitForExitAsync(cancel);

            _Logger.LogDebug($"downloaded engine [version={version}] [timer={timer.ElapsedMilliseconds}ms]");
        }

    }
}
