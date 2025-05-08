using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
		private readonly EnginePathUtil _EnginePathUtil;

        private static readonly HttpClient _Http = new HttpClient();

        private const string BASE_URL = "https://github.com/beyond-all-reason/spring/releases/download";

        private List<string> VERSION_PATH_TEMPLATES = [
            "{0}/spring_bar_.rel2501.{0}_{1}-64-minimal-portable.7z", // {0} => version, {1} => windows/linux
            "{0}/recoil_{0}_amd64-{1}.7z"
        ];

        static BarEngineDownloader() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

		public BarEngineDownloader(ILogger<BarEngineDownloader> logger,
			IOptions<FileStorageOptions> options, PathEnvironmentService pathUtil,
			EnginePathUtil enginePathUtil) {

			_Logger = logger;
			_Options = options;
			_PathUtil = pathUtil;
			_EnginePathUtil = enginePathUtil;
		}

		public bool HasEngine(string version) {
            string path = _EnginePathUtil.Get(version);
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
            string path = _EnginePathUtil.Get(version);
            if (Directory.Exists(path)) {
                _Logger.LogInformation($"engine version already downloaded [version={version}]");
                return;
            }

            Directory.CreateDirectory(path);

            Stopwatch timer = Stopwatch.StartNew();

            HttpResponseMessage? response = null;
            foreach (string template in VERSION_PATH_TEMPLATES) {
                string versionPath = string.Format(template, version, OperatingSystem.IsWindows() ? "windows" : "linux");
                _Logger.LogTrace($"trying to get engine version [template={template}] [versionPath={versionPath}]");
                response = await _Http.GetAsync(BASE_URL + "/" + versionPath);

                if (response.IsSuccessStatusCode == false) {
                    _Logger.LogWarning($"failed to download engine [version={version}] [status code={response.StatusCode}]");
                } else {
                    _Logger.LogInformation($"successfully downloaded engine [version={version}] [url={versionPath}]");
                    break;
                }
            }

            if (response == null) {
                _Logger.LogError($"failed to find engine version [version={version}]");
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
