using gex.Common.Models;
using gex.Common.Models.Familiar;
using gex.Common.Models.Options;
using gex.Common.Services;
using gex.Common.Services.Bar;
using gex.Familiar.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gex.Familiar.Services {

    public class GameInstance : IDisposable {

        private readonly ILogger<GameInstance> _Logger;
        private readonly EnginePathUtil _EnginePathUtil;
        private readonly BarEngineDownloader _EngineDownloader;
        private readonly PrDownloaderService _PrDownloader;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly IOptions<SpringFamiliarOptions> _SpringOptions;
        private readonly MatchUploader _Uploader;

        private Process? _BarInstance = null;

        /// <summary>
        ///     regex to find the messages from the widget that tell Gex what from the replay is on
        /// </summary>
        private static Regex FramePattern = new(@"^\[t=.*?\]\[f=\d*?\] \[Gex\] on frame (\d+?)$");

        /// <summary>
        ///     regex for an error that indicates the widget failed to load
        /// </summary>
        private static Regex GexLoadErrorPattern = new(@"\[t=.+?\]\[f=-000001\] Failed to load: gex\.lua");

        /// <summary>
        ///     regex for when the game ends
        /// </summary>
        private static Regex GameEndedPattern = new(@"\[t=.+?\]\[f=.+?\] \[SpringApp::Kill\]\[1\] fromRun=1");

        /// <summary>
        ///     regex for an error being printed
        /// </summary>
        private static Regex StdErrErrorPattern = new Regex(@"^\[t=.+?\]\[f=.+?\] Error: ");

        /// <summary>
        ///     regex for a fatal error being ran
        /// </summary>
        private static Regex StdErrFatalPattern = new Regex(@"^\[t=.+?\]\[f=.+?\] Fatal: ");

        /// <summary>
        ///     message for when an instance tried to bind to a port in use
        /// </summary>
        private const string SOCKET_BIND_ERROR = "Error: [UDPListener::TryBindSocket] binding UDP socket to IP localhost failed: "
            + "bind: An attempt was made to access a socket in a way forbidden by its access permissions.";

        public GameInstance(ILogger<GameInstance> logger,
            BarEngineDownloader engineDownloader, PrDownloaderService prDownloader,
            EnginePathUtil enginePathUtil, IOptions<FileStorageOptions> options,
            IOptions<SpringFamiliarOptions> springOptions, MatchUploader uploader) {

            _Logger = logger;
            _EngineDownloader = engineDownloader;
            _PrDownloader = prDownloader;
            _EnginePathUtil = enginePathUtil;
            _Options = options;
            _SpringOptions = springOptions;
            _Uploader = uploader;
        }

        public async Task<Result<bool, string>> Run(FamiliarLaunchGameMessage msg, CancellationToken cancel) {
            if (string.IsNullOrEmpty(msg.Engine) || string.IsNullOrEmpty(msg.GameVersion) || string.IsNullOrEmpty(msg.Map)) {
                _Logger.LogError($"invalid launch parameters used [engine={msg.Engine}] [version={msg.GameVersion}] [map={msg.Map}]");
                throw new Exception($"invalid launch message sent, refusing to start game");
            }

            int randomGameInstance = Random.Shared.Next();

            _Logger.LogInformation($"starting game [engine={msg.Engine}] [game version={msg.GameVersion}] [map={msg.Map}] [ip={msg.HostIP}] [port={msg.Port}]");

            if (_EngineDownloader.HasEngine(msg.Engine) == false) {
                _Logger.LogDebug($"missing engine, downloading [engine={msg.Engine}]");
                await _EngineDownloader.DownloadEngine(msg.Engine, cancel);
            }

            string enginePath = _EnginePathUtil.Get(msg.Engine);

            // ensure the widget (what the fuck is a wupget) is in the right place
            string widgetsDirectory = Path.Join(enginePath, "LuaUI", "Widgets");
            if (Directory.Exists(widgetsDirectory) == false) {
                Directory.CreateDirectory(widgetsDirectory);
            }

            string widgetLocation = widgetsDirectory + Path.DirectorySeparatorChar + "gex.lua";
            if (File.Exists(widgetLocation)) {
                File.Delete(widgetLocation);
            }
            File.Copy(Path.Join(_Options.Value.TempWorkLocation, "gex.lua"), Path.Join(widgetsDirectory, "gex.lua"));

            // ensure the widget is enabled for the first time run
            string dataDir = Path.Join(enginePath, $"data-{randomGameInstance}");
            string luaUiConfigDirectory = Path.Join(dataDir, "LuaUI", "Config");
            _Logger.LogDebug($"ensuring data dir contains LuaUI config that enables gex [luaUi={luaUiConfigDirectory}]");
            if (Directory.Exists(luaUiConfigDirectory) == false) {
                Directory.CreateDirectory(luaUiConfigDirectory);
            }
            string luaUiConfig = luaUiConfigDirectory + Path.DirectorySeparatorChar + "BYAR.lua";
            if (File.Exists(luaUiConfig) == true) {
                File.Delete(luaUiConfig);
            }
            File.Copy(Path.Join(_Options.Value.TempWorkLocation, "BYAR.lua"), luaUiConfig);

            int attempts = 3;

            do {
                if (cancel.IsCancellationRequested == true) {
                    break;
                }

                // ensure the game version is downloaded
                if (_PrDownloader.HasGameVersion(msg.Engine, msg.GameVersion) == false) {
                    _Logger.LogDebug($"missing game version, downloading [engine={msg.Engine}] [version={msg.GameVersion}]");
                    if ((await _PrDownloader.GetGameVersion(msg.Engine, msg.GameVersion, cancel)) == true) {
                        _Logger.LogDebug($"successfully downloaded game version [engine={msg.Engine}] [version={msg.GameVersion}]");
                        break;
                    }
                } else {
                    _Logger.LogDebug($"game version present [engine={msg.Engine}] [version={msg.GameVersion}]");
                    break;
                }
                _Logger.LogWarning($"failed to download game version, trying again [attempts={attempts}] [version={msg.GameVersion}]");
                --attempts;
            } while (attempts > 0);

            if (_PrDownloader.HasGameVersion(msg.Engine, msg.GameVersion) == false) {
                _Logger.LogError($"failed to download game version [engine={msg.Engine}] [version={msg.GameVersion}]");
                return $"failed to download game version ({msg.GameVersion})";
            }

            // ensure map is downloaded
            if (_PrDownloader.HasMap(msg.Engine, msg.Map) == false) {
                _Logger.LogDebug($"missing map, downloading [engine={msg.Engine}] [map={msg.Map}]");
                await _PrDownloader.GetMap(msg.Engine, msg.Map, cancel);
            }

            if (_PrDownloader.HasMap(msg.Engine, msg.Map) == false) {
                _Logger.LogError($"failed to download map [engine={msg.Engine}] [map={msg.Map}]");
                return $"failed to download map ({msg.Map})";
            }

            // setup script file to be ran
            string scriptsFile = Path.Join(enginePath, $"_script-{randomGameInstance}.txt");
            if (File.Exists(scriptsFile) == true) {
                File.Delete(scriptsFile);
            }

            await File.WriteAllTextAsync(scriptsFile, $"[game] {{\nHostIP={msg.HostIP};HostPort={msg.Port};SourcePort=0;"
                + $"MyPlayerName={_SpringOptions.Value.Username};MyPasswd={msg.Secret};IsHost=0;\n}}", cancel);

            // actually run the process now that everything is setup
            using Process bar = new();
            _BarInstance = bar;
            bar.StartInfo.FileName = Path.Join(enginePath, "spring-headless");
            if (OperatingSystem.IsWindows()) { bar.StartInfo.FileName += ".exe"; }
            bar.StartInfo.WorkingDirectory = enginePath;
            bar.StartInfo.Arguments = $"--write-dir \"{dataDir}\" \"{scriptsFile}\"";
            bar.StartInfo.UseShellExecute = false;
            bar.StartInfo.RedirectStandardOutput = true;
            bar.StartInfo.RedirectStandardError = true;

            // this callback is Dispose-able, so even tho we don't use this, we still want to capture it for Disposale
            using CancellationTokenRegistration cancelCallback = cancel.Register(() => {
                _Logger.LogInformation($"killing BAR instance due to cancellation");
                bar.Kill();
            });

            Stopwatch timer = Stopwatch.StartNew();
            long playbackStartedMs = 0;

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            DateTime playbackStarted = DateTime.UtcNow;
            TimeSpan processingTimeout = TimeSpan.FromHours(2);
            DateTime timeEnd = playbackStarted + processingTimeout;

            _Logger.LogDebug($"starting bar executable "
                + $"[cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}] [timeout={processingTimeout}] [end={timeEnd:u}]");

            long previousTimerUpdate = 0;
            long previousFrame = 0;

            bool gameEnded = false;
            using AutoResetEvent outputWaitHandle = new(false);
            using AutoResetEvent errorWaitHandle = new(false);
            bar.OutputDataReceived += (sender, e) => {
                if (e.Data == null) {
                    outputWaitHandle.Set();
                } else {
                    output.AppendLine(e.Data);

                    // [t=00:00:46.459234][f=-000001] Failed to load: gex.lua
                    Match errorMatch = GexLoadErrorPattern.Match(e.Data);
                    if (errorMatch.Success == true) {
                        _Logger.LogError($"Gex Lua addon was not loaded! killing instance [error={e.Data}]");
                        bar.Kill();
                    }

                    Match gameEndedMatch = GameEndedPattern.Match(e.Data);
                    if (gameEndedMatch.Success == true) {
                        _Logger.LogDebug($"game ended, ignoring any errors past this");
                        gameEnded = true;
                    }

                    Match m = FramePattern.Match(e.Data);
                    if (m.Success == false) {
                        return;
                    }

                    if (m.Groups.Count < 2) {
                        _Logger.LogWarning($"expected more groups [groups.Count={m.Groups.Count}]");
                        return;
                    }

                    string frameStr = m.Groups[1].Value;
                    if (long.TryParse(frameStr, out long frame) == false) {
                        _Logger.LogWarning($"failed to parse frame string into a long [frame={frameStr}]");
                        return;
                    }

                    long replayTimer = timer.ElapsedMilliseconds - playbackStartedMs;
                    long deltaFrame = frame - previousFrame;
                    long deltaTimer = replayTimer - previousTimerUpdate;

                    double gameFps = 0d;

                    if (frame == 0) {
                        _Logger.LogDebug($"game startup complete [timer={timer.ElapsedMilliseconds}ms] [engine={msg.Engine}] [version={msg.GameVersion}]");
                        playbackStartedMs = timer.ElapsedMilliseconds;
                    } else {
                        decimal fps = deltaFrame / (Math.Max(1, deltaTimer) / 1000m);
                        gameFps = (double)fps;
                        _Logger.LogDebug($"game frame update sent [frame={frame}] [timer={timer.ElapsedMilliseconds}ms] [speedup={fps / 30m:F3}] [fps={fps:F3}]");
                    }

                    previousTimerUpdate = timer.ElapsedMilliseconds - playbackStartedMs;
                    previousFrame = frame;
                }
            };
            bar.ErrorDataReceived += (sender, e) => {
                if (e.Data == null) {
                    errorWaitHandle.Set();
                } else {
                    error.AppendLine(e.Data);

                    // [t=00:00:00.104862] Fatal: [ExitSpringProcess] errorMsg="[thread::error::run] not enough free space on drive containing writeable data-directory <path> msgCaption="Spring: caught std::exception" mainThread=1

                    if (e.Data.Contains("Failed to load: gex.lua")) {
                        _Logger.LogError($"Gex Lua addon was not loaded! killing instance [error={e.Data}]");
                        bar.Kill();
                    }

                    if (e.Data.Contains(SOCKET_BIND_ERROR)) {
                        _Logger.LogError($"BAR failed to bind to port [error={e.Data}]");
                        bar.Kill();
                    }

                    if (gameEnded == true) {
                        return;
                    }

                    Match m = StdErrErrorPattern.Match(e.Data);
                    if (m.Success == true) {
                        //_Logger.LogError($"Error from stderr caught [gameID={gameID}] [msg={e.Data}]");
                    }

                    m = StdErrFatalPattern.Match(e.Data);
                    if (m.Success == true) {
                        _Logger.LogError($"Fatal error from stderr caught [msg={e.Data}]");
                    }
                }
            };

            bar.Start();

            bar.BeginOutputReadLine();
            bar.BeginErrorReadLine();

            // doing it this way prevents hangs due to not reading stdout or stderr
            // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            if (!(bar.WaitForExit(processingTimeout) && outputWaitHandle.WaitOne(processingTimeout) && errorWaitHandle.WaitOne(processingTimeout))) {
                _Logger.LogWarning($"hit game processing timeout [timeout={processingTimeout}]");
            }

            // game successfully ran, good job gex!
            _Logger.LogInformation($"BAR match ran locally [timer={timer.ElapsedMilliseconds}ms] "
                + $"[startup={playbackStartedMs}ms] [replay={timer.ElapsedMilliseconds - playbackStartedMs}ms]");

            string demofileDir = Path.Join(dataDir, "demos");
            string[] demofileDirFiles = Directory.GetFiles(demofileDir);
            if (demofileDirFiles.Length != 1) {
                throw new Exception($"expected exactly 1 file in {demofileDir}, had {demofileDirFiles.Length} files");
            }
            string demofileFile = demofileDirFiles[0];

            _Logger.LogDebug($"found demofile [demofileFile={demofileFile}]");
            byte[] demofile = await File.ReadAllBytesAsync(demofileFile);

            byte[] actions = Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(Path.Join(dataDir, "actions.json")));

            await _Uploader.Upload(demofile, actions,
                Encoding.UTF8.GetBytes(output.ToString()),
                Encoding.UTF8.GetBytes(error.ToString())
            );

            try {
                File.Delete(scriptsFile);
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to delete script [path={scriptsFile}] [ex={ex.Message}]");
            }

            return true;
        }

        public void Kill() {
            _BarInstance?.Kill();
        }

        public void Dispose() {
            _BarInstance?.Kill();
            _BarInstance?.Dispose();
        }

    }
}
