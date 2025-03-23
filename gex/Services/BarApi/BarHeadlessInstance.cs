using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarHeadlessInstance {

        private readonly ILogger<BarHeadlessInstance> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarMatchRepository _MatchRepository;
        private readonly PrDownloaderService _PrDownloader;
        private readonly BarEngineDownloader _EngineDownloader;

        private static int _PortOffset = 0;

        public BarHeadlessInstance(ILogger<BarHeadlessInstance> logger,
            IOptions<FileStorageOptions> options, BarMatchRepository matchRepository,
            PrDownloaderService prDownloader, BarEngineDownloader engineDownloader) {

            _Logger = logger;
            _Options = options;
            _MatchRepository = matchRepository;
            _PrDownloader = prDownloader;
            _EngineDownloader = engineDownloader;
        }

        public async Task<Result<GameOutput, string>> RunGame(string gameID, bool force, CancellationToken cancel) {

            _Logger.LogDebug($"starting BAR headless instance [gameID={gameID}] [cwd={Environment.CurrentDirectory}]");

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return $"cannot find game with ID '{gameID}'";
            }

            string gameLogLocation = Path.Join(_Options.Value.GameLogLocation, gameID);
            string gameActionLogPath = gameLogLocation + Path.DirectorySeparatorChar + "actions.json";
            if (File.Exists(gameActionLogPath) && force == false) {
                _Logger.LogInformation($"game has already been ran locally (actions.json exists) [gameID={gameID}] [action log={gameActionLogPath}]");
                return new GameOutput() { GameID = gameID };
            }
            if (File.Exists(gameActionLogPath) == true) {
                _Logger.LogInformation($"action log was already created for game, but execution was forced [gameID={gameID}] [action log={gameActionLogPath}]");
                File.Delete(gameActionLogPath);
            }

            // make sure the demofile exists
            string demofileLocation = Path.Join(_Options.Value.ReplayLocation, match.FileName);
            if (File.Exists(demofileLocation) == false) {
                return $"cannot find demofile at '{demofileLocation}'";
            }

            _Logger.LogDebug($"loaded match for headless replay [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}] [map={match.Map}] [filename={match.FileName}]");

            if (_EngineDownloader.HasEngine(match.Engine) == false) {
                _Logger.LogDebug($"missing engine, downloading [gameID={gameID}] [engine={match.Engine}]");
                await _EngineDownloader.DownloadEngine(match.Engine, cancel);
            }

            string enginePath = GetEnginePath(match.Engine);

            // ensure the widget (what the fuck is a wupget) is in the right place
            string widgetsDirectory = Path.Join(enginePath, "LuaUI", "Widgets");
            if (Directory.Exists(widgetsDirectory) == false) {
                Directory.CreateDirectory(widgetsDirectory);
            }

            string widgetLocation = widgetsDirectory + Path.DirectorySeparatorChar + "gex.lua";
            if (File.Exists(widgetLocation)) {
                File.Delete(widgetLocation);
            }
            File.Copy("./gex.lua", widgetsDirectory + Path.DirectorySeparatorChar + "gex.lua");

            // ensure the widget is enabled for the first time run
            string dataDir = Path.Join(enginePath, $"data-{gameID}");
            string luaUiConfigDirectory = Path.Join(dataDir, "LuaUI", "Config");
            _Logger.LogDebug($"ensuring data dir contains LuaUI config that enables gex [gameID={gameID}] [luaUi={luaUiConfigDirectory}]");
            if (Directory.Exists(luaUiConfigDirectory) == false) {
                Directory.CreateDirectory(luaUiConfigDirectory);
            }
            string luaUiConfig = luaUiConfigDirectory + Path.DirectorySeparatorChar + "BYAR.lua";
            if (File.Exists(luaUiConfig) == true) {
                File.Delete(luaUiConfig);
            }
            File.Copy("./BYAR.lua", luaUiConfig);

            // ensure the game version is downloaded
            if (_PrDownloader.HasGameVersion(match.Engine, match.GameVersion) == false) {
                _Logger.LogDebug($"missing game version, downloading [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}]");
                await _PrDownloader.GetGameVersion(match.Engine, match.GameVersion, cancel);
            }

            // ensure map is downloaded
            if (_PrDownloader.HasMap(match.Engine, match.Map) == false) {
                _Logger.LogDebug($"missing map, downloading [gameID={gameID}] [engine={match.Engine}] [map={match.Map}]");
                await _PrDownloader.GetMap(match.Engine, match.Map, cancel);
            }

            // setup script file to be ran
            string scriptsFile = Path.Join(enginePath, $"_script-{gameID}.txt");
            if (File.Exists(scriptsFile) == true) {
                File.Delete(scriptsFile);
            }

            int port = 50000 + (_PortOffset++ % 1000);

            await File.WriteAllTextAsync(scriptsFile, $"[game] {{\ndemofile={demofileLocation};HostPort={port};\n}}", cancel);

            // actually run the process now that everything is setup
            using Process bar = new();
            bar.StartInfo.FileName = Path.Join(enginePath, "spring-headless");
            if (OperatingSystem.IsWindows()) { bar.StartInfo.FileName += ".exe"; }
            bar.StartInfo.WorkingDirectory = enginePath;
            bar.StartInfo.Arguments = $"--write-dir \"{dataDir}\" \"{scriptsFile}\"";
            bar.StartInfo.UseShellExecute = false;
            bar.StartInfo.RedirectStandardOutput = true;
            bar.StartInfo.RedirectStandardError = true;

            cancel.Register(() => {
                _Logger.LogInformation($"killing BAR instance due to cancellation [gameID={gameID}]");
                bar.Kill();
            });

            _Logger.LogDebug($"starting bar executable [gameID={gameID}] [port={port}] [cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}]");

            Stopwatch timer = Stopwatch.StartNew();

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            TimeSpan processingTimeout = TimeSpan.FromMilliseconds(match.DurationMs);

            using AutoResetEvent outputWaitHandle = new(false);
            using AutoResetEvent errorWaitHandle = new(false);
            bar.OutputDataReceived += (sender, e) => {
                if (e.Data == null) {
                    outputWaitHandle.Set();
                } else {
                    output.AppendLine(e.Data);
                }
            };
            bar.ErrorDataReceived += (sender, e) => {
                if (e.Data == null) {
                    errorWaitHandle.Set();
                } else {
                    error.AppendLine(e.Data);
                }
            };

            bar.Start();

            bar.BeginOutputReadLine();
            bar.BeginErrorReadLine();

            // doing it this way prevents hangs due to not reading stdout or stderr
            // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            if (!(bar.WaitForExit(processingTimeout) && outputWaitHandle.WaitOne(processingTimeout) && errorWaitHandle.WaitOne(processingTimeout))) {
                _Logger.LogWarning($"timeout!");
                return "took longer to process the game than the game ran, something went wrong!";
            }

            // game successfully ran, good job gex!
            _Logger.LogInformation($"BAR match ran locally [gameID={gameID}] [timer={timer.ElapsedMilliseconds}ms]");

            try {
                File.Delete(scriptsFile);
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to delete script [gameID={gameID}] [path={scriptsFile}] [ex={ex.Message}]");
            }

            // copy logs to folder
            if (Directory.Exists(gameLogLocation) == false) {
                Directory.CreateDirectory(gameLogLocation);
            }

            string stdoutLogs = gameLogLocation + Path.DirectorySeparatorChar + "stdout.txt";
            string stderrLogs = gameLogLocation + Path.DirectorySeparatorChar + "stderr.txt";
            await File.WriteAllTextAsync(stdoutLogs, output.ToString(), cancel);
            await File.WriteAllTextAsync(stderrLogs, error.ToString(), cancel);

            string actionLogLocation = Path.Join(dataDir, "actions.json");
            if (File.Exists(actionLogLocation) == false) {
                return $"failed to find action log after game ran! {actionLogLocation} was missing";
            }

            File.Copy(actionLogLocation, gameLogLocation + Path.DirectorySeparatorChar + "actions.json");
            _Logger.LogDebug($"game ran and output copied, deleting data dir [gameID={gameID}] [dataDir={dataDir}]");
            try {
                Directory.Delete(dataDir, true);
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to delete data dir after run! [gameID={gameID}] [dataDir={dataDir}] [ex={ex.Message}]");
            }

            return new GameOutput() { GameID = gameID };
        }

        private string GetEnginePath(string version) {
            string path = _Options.Value.EngineLocation + Path.DirectorySeparatorChar + version;

            if (OperatingSystem.IsWindows() == true) {
                path += "-win";
            } else if (OperatingSystem.IsLinux() == true) {
                path += "-linux";
            } else {
                _Logger.LogWarning($"unchecked operating system [is android={OperatingSystem.IsAndroid()}] [is bsd={OperatingSystem.IsFreeBSD()}]");
            }

            return path;
        }

    }
}
