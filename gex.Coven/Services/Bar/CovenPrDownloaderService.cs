using gex.Common.Code;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Services.Bar;
using gex.Coven.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Bar {

    public class CovenPrDownloaderService : IPrDownloaderService {

        private readonly ILogger<CovenPrDownloaderService> _Logger;
        private readonly UserOptionsService _UserOptionsService;

        private static Dictionary<string, Task<bool>> _PendingDownloads = [];

        public CovenPrDownloaderService(ILogger<CovenPrDownloaderService> logger,
            UserOptionsService userOptionsService) {

            _Logger = logger;
            _UserOptionsService = userOptionsService;
        }

        public bool HasMap(string engine, string map) {
            UserOptions userOptions = _UserOptionsService.Load();

            string mapName = (map + ".sd7").EscapeRecoilFilesytemCharacters().ToLower();
            string path = Path.Join(userOptions.InstallFolder, "maps", mapName);

            return File.Exists(path);
        }

        public Task GetMap(string engine, string mapName, CancellationToken cancel) {
            if (HasMap(engine, mapName) == true) {
                return Task.CompletedTask;
            }

            UserOptions userOptions = _UserOptionsService.Load();
            string mapOutput = Path.Join(userOptions.InstallFolder, "maps");
            ProcessStartInfo startInfo = GetForEngine(engine, $"--filesystem-writepath \"{mapOutput}\" --download-map \"{mapName}\"");

            ProcessWrapper proc = ProcessWrapper.Create(startInfo, TimeSpan.FromMinutes(2));
            int exitCode = proc.ExitCode;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> GetGameVersion(string engine, string version, CancellationToken cancel) {
            if (HasGameVersion(engine, version) == true) {
                _Logger.LogDebug($"game version already downloaded [engine={engine}] [version={version}]");
                return true;
            }

            string downloadKey = $"{engine}-{version}";

            Task<bool>? pendingDownload = null;
            lock (_PendingDownloads) {
                pendingDownload = _PendingDownloads.GetValueOrDefault(downloadKey);
            }

            if (pendingDownload != null) {
                _Logger.LogInformation($"game version download is already occuring [engine={engine}] [version={version}]");
                return await pendingDownload;
            }

            lock (_PendingDownloads) {
                pendingDownload = GetGameVersionInternal(engine, version, cancel);
                _PendingDownloads.Add(downloadKey, pendingDownload);
            }
            try {
                return await pendingDownload;
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to download game version [engine={engine}] [version={version}]");
                return false;
            } finally {
                lock (_PendingDownloads) {
                    _PendingDownloads.Remove(downloadKey);
                }
            }
        }

        private async Task<bool> GetGameVersionInternal(string engine, string version, CancellationToken cancel) {
            UserOptions userOptions = _UserOptionsService.Load();
            string gameVersionOutput = Path.Join(userOptions.InstallFolder, "engine", engine, "games", version);

            if (Directory.Exists(gameVersionOutput) == true) {
                _Logger.LogInformation($"incomplete game version downloaded found [engine={engine}] [version={version}] [path={gameVersionOutput}]");
                Directory.Delete(gameVersionOutput, true);
            }

            Directory.CreateDirectory(gameVersionOutput);

            // pr-downloader.exe --filesystem-writepath "data" --download-game "Beyond All Reason test-27562-33e445c"
            ProcessStartInfo startInfo = GetForEngine(engine, $"--filesystem-writepath \"{gameVersionOutput}\" --download-game \"{version}\"");
            startInfo.WorkingDirectory = Path.Join(userOptions.InstallFolder, "engine", engine, "games", version);
            _Logger.LogInformation($"downloading game version [version={version}] [engine={engine}]");
            _Logger.LogDebug($"getting game version [version={version}] [engine={engine}] [args={startInfo.Arguments}]");

            Stopwatch timer = Stopwatch.StartNew();
            ProcessWrapper proc = ProcessWrapper.Create(startInfo, TimeSpan.FromMinutes(5));
            int exitCode = proc.ExitCode;

            if (exitCode != 0) {
                _Logger.LogError($"failed to download game version [version={version}] [engine={engine}]:\nstdout: {proc.StdOut}\nstderr: {proc.StdErr}");
                return false;
            }

            // 2025-04-06 TODO: apparently something like this can be used to validate the download
            //      pr-downloader.exe --filesystem-writepath "F:\Gex\Engines\2025.01.6-win\games\Beyond All Reason test-27756-37edc11" --rapid-validate 

            // to prevent partial downloads, a done file is created after the game version is downloaded
            // to mark a completed game version download. this file is what's checked for 
            // when determining if a game version already exists or not
            using FileStream done = File.OpenWrite(gameVersionOutput + Path.DirectorySeparatorChar + "done.txt");
            await done.WriteAsync(new byte[] { 0x00 }, cancel);

            _Logger.LogInformation($"game version fetched [version={version}] [engine={engine}] [timer={timer.ElapsedMilliseconds}ms] [exit code={exitCode}]");

            return true;
        }

        /// <inheritdoc/>
        public bool HasGameVersion(string engine, string version) {
            UserOptions userOptions = _UserOptionsService.Load();
            string gameVersionOutput = Path.Join(userOptions.InstallFolder, "engine", engine, "games", version, "done.txt");
            return File.Exists(gameVersionOutput);
        }

        private ProcessStartInfo GetForEngine(string engine, string arguments) {
            UserOptions userOptions = _UserOptionsService.Load();

            string path = Path.Join(userOptions.InstallFolder, "engine", engine, "pr-downloader");
            if (OperatingSystem.IsWindows()) { path += ".exe"; }

            if (File.Exists(path) == false) {
                throw new Exception($"failed to find pr-downloader in '{path}'");
            }

            ProcessStartInfo startInfo = new();
            startInfo.FileName = path;
            startInfo.WorkingDirectory = Path.Join(userOptions.InstallFolder);
            startInfo.EnvironmentVariables.AddOrUpdate("PRD_RAPID_USE_STREAMER", "false");
            startInfo.EnvironmentVariables.AddOrUpdate("PRD_HTTP_SEARCH_URL", "https://files-cdn.beyondallreason.dev/find");
            startInfo.EnvironmentVariables.AddOrUpdate("PRD_RAPID_REPO_MASTER", "https://repos-cdn.beyondallreason.dev/repos.gz");
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            return startInfo;
        }

    }
}
