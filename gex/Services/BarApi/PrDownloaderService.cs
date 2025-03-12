using gex.Code;
using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class PrDownloaderService {

        private readonly ILogger<PrDownloaderService> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        public PrDownloaderService(ILogger<PrDownloaderService> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        ///     check if a game version for a specific engine has been downloaded
        /// </summary>
        /// <param name="engine">version of the engine</param>
        /// <param name="version">game version</param>
        /// <returns>
        ///     a boolean value indicating if the game version has already been downloaded for the specified engine
        /// </returns>
        public bool HasGameVersion(string engine, string version) {
            string gameVersionOutput = Path.Join(_Options.Value.EngineLocation, engine, "games", version, "done.txt");
            return File.Exists(gameVersionOutput);
        }

        /// <summary>
        ///     download a specific game version for an engine
        /// </summary>
        /// <param name="engine">engine that will store the game version</param>
        /// <param name="version">game version to be downloaded</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task GetGameVersion(string engine, string version, CancellationToken cancel) {
            if (HasGameVersion(engine, version) == true) {
                _Logger.LogDebug($"game version already downloaded [engine={engine}] [version={version}]");
                return;
            }

            string gameVersionOutput = Path.Join(_Options.Value.EngineLocation, engine, "games", version);

            if (Directory.Exists(gameVersionOutput) == true) {
                _Logger.LogInformation($"incomplete game version downloaded found [engine={engine}] [version={version}] [path={gameVersionOutput}]");
                Directory.Delete(gameVersionOutput, true);
            }

            // pr-downloader.exe --filesystem-writepath "data" --download-game "Beyond All Reason test-27562-33e445c"
            using Process proc = new Process();
            proc.StartInfo = GetForEngine(engine, $"--filesystem-writepath \"{gameVersionOutput}\" --download-game \"{version}\"");
            _Logger.LogDebug($"getting game version [version={version}] [engine={engine}] [args={proc.StartInfo.Arguments}]");

            Stopwatch timer = Stopwatch.StartNew();
            proc.Start();
            string stdout = await proc.StandardOutput.ReadToEndAsync(cancel);
            await proc.WaitForExitAsync(cancel);

            // to prevent partial downloads, a done file is created after the game version is downloaded
            // to mark a completed game version download. this file is what's checked for 
            // when determining if a game version already exists or not
            using FileStream done = File.OpenWrite(gameVersionOutput + Path.DirectorySeparatorChar + "done.txt");
            await done.WriteAsync(new byte[] { 0x00 }, cancel);

            _Logger.LogInformation($"game version fetched [version={version}] [engine={engine}] [timer={timer.ElapsedMilliseconds}ms]");

        }

        /// <summary>
        ///     check if a map has already been downloaded for a specific engine version
        /// </summary>
        /// <param name="engine">name of the engine</param>
        /// <param name="map">name of the map</param>
        /// <returns>
        ///     if the map has already been saved for the passed engine
        /// </returns>
        public bool HasMap(string engine, string map) {
            string mapName = (map + ".sd7").Replace(" ", "_").ToLower();
            string path = Path.Join(_Options.Value.EngineLocation, engine, "maps", "maps", mapName);
            return File.Exists(path);
        }

        /// <summary>
        ///     download a map for a specific engine version
        /// </summary>
        /// <param name="engine">version of the engine to get the map for</param>
        /// <param name="mapName">name of the map to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task GetMap(string engine, string mapName, CancellationToken cancel) {

            if (HasMap(engine, mapName) == true) {
                _Logger.LogDebug($"map already downloaded [engine={engine}] [map={mapName}]");
                return;
            }

            // pr-downloader.exe --filesystem-writepath "./maps" --download-map "All That Glitters v2.2"

            string mapOutput = Path.Join(_Options.Value.EngineLocation, engine, "maps");

            ProcessStartInfo startInfo = GetForEngine(engine, $"--filesystem-writepath \"{mapOutput}\" --download-map \"{mapName}\"");
            _Logger.LogDebug($"getting map [version={mapName}] [engine={engine}] [args={startInfo.Arguments}]");

            Stopwatch timer = Stopwatch.StartNew();
            ProcessWrapper proc = ProcessWrapper.Create(startInfo, TimeSpan.FromMinutes(2));

            if (HasMap(engine, mapName) == false) {
                throw new Exception($"expected map to exist [map={mapName}] [engine={engine}]");
            }

            _Logger.LogInformation($"map fetched [map={mapName}] [engine={engine}] [timer={timer.ElapsedMilliseconds}ms]");
        }

        private ProcessStartInfo GetForEngine(string engine, string arguments) {

            string path = _Options.Value.EngineLocation + Path.DirectorySeparatorChar + engine + Path.DirectorySeparatorChar + "pr-downloader";
            if (OperatingSystem.IsWindows()) { path += ".exe"; }

            if (File.Exists(path) == false) {
                throw new Exception($"failed to find pr-downloader in '{path}'");
            }

            ProcessStartInfo startInfo = new();
            startInfo.FileName = path;
            startInfo.WorkingDirectory = _Options.Value.EngineLocation + Path.DirectorySeparatorChar + engine;
            startInfo.EnvironmentVariables.Add("PRD_RAPID_USE_STREAMER", "false");
            startInfo.EnvironmentVariables.Add("PRD_HTTP_SEARCH_URL", "https://files-cdn.beyondallreason.dev/find");
            startInfo.EnvironmentVariables.Add("PRD_RAPID_REPO_MASTER", "https://repos-cdn.beyondallreason.dev/repos.gz");
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            return startInfo;
        }

    }
}
