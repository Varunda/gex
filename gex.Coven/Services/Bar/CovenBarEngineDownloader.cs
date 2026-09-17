using gex.Common.Services.Bar;
using gex.Coven.Models.Config;
using Microsoft.Extensions.Logging;
using SevenZip;
using SevenZip.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Bar {

    public class CovenBarEngineDownloader : IBarEngineDownloader {

        private readonly ILogger<CovenBarEngineDownloader> _Logger;
        private readonly UserOptionsService _UserOptionsService;

        private static readonly HttpClient _Http = new HttpClient();

        private const string BASE_URL = "https://github.com/beyond-all-reason/spring/releases/download";

        private List<string> VERSION_PATH_TEMPLATES = [
            "{0}/spring_bar_.rel2501.{0}_{1}-64-minimal-portable.7z", // {0} => version, {1} => windows/linux
            "{0}/recoil_{0}_amd64-{1}.7z"
        ];

        static CovenBarEngineDownloader() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex.Coven/0.1");
        }

        public CovenBarEngineDownloader(ILogger<CovenBarEngineDownloader> logger,
            UserOptionsService userOptionsService) {

            _Logger = logger;
            _UserOptionsService = userOptionsService;
        }

        public bool HasEngine(string version) {
            UserOptions userOptions = _UserOptionsService.Load();
            string enginePath = Path.Join(userOptions.InstallFolder, "engine", version);

            return Directory.Exists(enginePath);
        }

        public async Task DownloadEngine(string version, CancellationToken cancel) {
            UserOptions userOptions = _UserOptionsService.Load();
            string enginePath = Path.Join(userOptions.InstallFolder, "engine", version);
            if (Directory.Exists(enginePath)) {
                _Logger.LogInformation($"engine version already downloaded [version={version}]");
                return;
            }

            Directory.CreateDirectory(enginePath);

            Stopwatch timer = Stopwatch.StartNew();

            HttpResponseMessage? response = null;
            foreach (string template in VERSION_PATH_TEMPLATES) {
                string versionPath = string.Format(template, version, OperatingSystem.IsWindows() ? "windows" : "linux");
                _Logger.LogTrace($"trying to get engine version [template={template}] [versionPath={versionPath}]");
                response = await _Http.GetAsync(BASE_URL + "/" + versionPath, cancel);

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

            string outputPath = enginePath + Path.DirectorySeparatorChar + "engine.7z";
            using (FileStream output = File.OpenWrite(outputPath)) {
                await response.Content.CopyToAsync(output, cancel);
            }

            using ArchiveReader reader = new ArchiveReader(outputPath);
            reader.ExtractAll(enginePath);

            _Logger.LogDebug($"downloaded engine [version={version}] [timer={timer.ElapsedMilliseconds}ms]");
        }

    }
}
