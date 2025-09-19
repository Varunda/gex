using gex.Code.Hubs;
using gex.Models;
using gex.Models.Api;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Services.Db;
using gex.Services.Metrics;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.AspNetCore.SignalR;
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
        private readonly EnginePathUtil _EnginePathUtil;
        private readonly GameVersionUsageDb _VersionUsageDb;
        private readonly HeadlessRunStatusRepository _HeadlessRunStatusRepository;
        private readonly BaseQueue<HeadlessRunStatus> _HeadlessRunStatusQueue;
        private readonly IHubContext<HeadlessReplayHub, IHeadlessReplayHub> _HeadlessReplayHub;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly HeadlessRunnerMetric _Metric;

        private static int _PortOffset = 0;

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

        public BarHeadlessInstance(ILogger<BarHeadlessInstance> logger,
            IOptions<FileStorageOptions> options, BarMatchRepository matchRepository,
            PrDownloaderService prDownloader, BarEngineDownloader engineDownloader,
            EnginePathUtil enginePathUtil, GameVersionUsageDb versionUsageDb,
            HeadlessRunStatusRepository headlessRunStatusRepository, BaseQueue<HeadlessRunStatus> headlessRunStatusQueue,
            IHubContext<HeadlessReplayHub, IHeadlessReplayHub> headlessReplayHub,
            BarMatchProcessingRepository processingRepository, HeadlessRunnerMetric metric) {

            _Logger = logger;
            _Options = options;
            _MatchRepository = matchRepository;
            _PrDownloader = prDownloader;
            _EngineDownloader = engineDownloader;
            _EnginePathUtil = enginePathUtil;
            _VersionUsageDb = versionUsageDb;
            _HeadlessRunStatusRepository = headlessRunStatusRepository;
            _HeadlessRunStatusQueue = headlessRunStatusQueue;
            _HeadlessReplayHub = headlessReplayHub;
            _ProcessingRepository = processingRepository;
            _Metric = metric;
        }

        public async Task<Result<GameOutput, string>> RunGame(string gameID, bool force, CancellationToken cancel) {

            _Logger.LogDebug($"starting BAR headless instance [gameID={gameID}] [cwd={Environment.CurrentDirectory}]");

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return $"cannot find game with ID '{gameID}' (missing from repository)";
            }

            string gamePrefixLocation = Path.Join(_Options.Value.GameLogLocation, gameID.Substring(0, 2));
            Directory.CreateDirectory(gamePrefixLocation);

            string gameLogLocation = Path.Join(gamePrefixLocation, gameID);
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

            FileInfo demofileInfo = new(demofileLocation);
            if (demofileInfo.Length > 1024 * 1024 * 64) {
                _Logger.LogError($"demofile size is larger than expected, refusing to process [gameID={gameID}] [size={demofileInfo.Length}]");
                return $"demofile size is too large, refusing to processing";
            }

            _Logger.LogDebug($"loaded match for headless replay [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}] [map={match.Map}] [filename={match.FileName}]");

            if (_EngineDownloader.HasEngine(match.Engine) == false) {
                _Logger.LogDebug($"missing engine, downloading [gameID={gameID}] [engine={match.Engine}]");
                await _EngineDownloader.DownloadEngine(match.Engine, cancel);
            }

            string enginePath = _EnginePathUtil.Get(match.Engine);

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

            int attempts = 3;

            do {
                if (cancel.IsCancellationRequested == true) {
                    break;
                }

                // ensure the game version is downloaded
                if (_PrDownloader.HasGameVersion(match.Engine, match.GameVersion) == false) {
                    _Logger.LogDebug($"missing game version, downloading [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}]");
                    if ((await _PrDownloader.GetGameVersion(match.Engine, match.GameVersion, cancel)) == true) {
                        _Logger.LogDebug($"successfully downloaded game version [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}]");
                        break;
                    }
                } else {
                    _Logger.LogDebug($"game version present [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}]");
                    break;
                }
                _Logger.LogWarning($"failed to download game version, trying again [attempts={attempts}] [gameID={gameID}] [version={match.GameVersion}]");
                --attempts;
            } while (attempts > 0);

            if (_PrDownloader.HasGameVersion(match.Engine, match.GameVersion) == false) {
                _Logger.LogError($"failed to download game version [gameID={gameID}] [engine={match.Engine}] [version={match.GameVersion}]");
                return $"failed to download game version ({match.GameVersion})";
            }

            await _VersionUsageDb.Upsert(new GameVersionUsage() {
                Engine = match.Engine,
                Version = match.GameVersion,
                LastUsed = DateTime.UtcNow
            }, cancel);

            // ensure map is downloaded
            if (_PrDownloader.HasMap(match.Engine, match.Map) == false) {
                _Logger.LogDebug($"missing map, downloading [gameID={gameID}] [engine={match.Engine}] [map={match.Map}]");
                await _PrDownloader.GetMap(match.Engine, match.Map, cancel);
            }

            if (_PrDownloader.HasMap(match.Engine, match.Map) == false) {
                _Logger.LogError($"failed to download map [gameID={gameID}] [engine={match.Engine}] [map={match.Map}]");
                return $"failed to download map ({match.Map})";
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

            // this callback is Dispose-able, so even tho we don't use this, we still want to capture it for Disposale
            using CancellationTokenRegistration cancelCallback = cancel.Register(() => {
                _Logger.LogInformation($"killing BAR instance due to cancellation [gameID={gameID}]");
                bar.Kill();
            });

            Stopwatch timer = Stopwatch.StartNew();
            long playbackStartedMs = 0;

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            // cap replay time to 10 minutes, unless execution was forced, in which case cap at the runtime of the match
            TimeSpan processingTimeout = force == true ? TimeSpan.FromMilliseconds(match.DurationMs) : TimeSpan.FromMinutes(10);

            // if gex thinks the game will take longer to replay than is left in the timeout, it will kill the game early
            int etaFailedCheck = 0;
            DateTime playbackStarted = DateTime.UtcNow;
            DateTime timeEnd = playbackStarted + processingTimeout;

            _Logger.LogDebug($"starting bar executable [gameID={gameID}] [port={port}] "
                + $"[cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}] [timeout={processingTimeout}] [end={timeEnd:u}]");

            HeadlessRunStatus status = new();
            status.GameID = gameID;
            status.Timestamp = DateTime.UtcNow;
            status.Frame = 0;
            status.DurationFrames = match.DurationFrameCount;
            status.Simulating = false;
            status.Fps = 0d;
            _HeadlessRunStatusRepository.Upsert(gameID, status);
            _HeadlessRunStatusQueue.Queue(status);

            long previousTimerUpdate = 0;
            long previousFrame = 0;

            bool increasePriority = false;

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
                        _Logger.LogError($"Gex Lua addon was not loaded! killing instance [gameID={gameID}] [error={e.Data}]");
                        _HeadlessRunStatusRepository.Remove(gameID);
                        bar.Kill();
                    }

                    Match gameEndedMatch = GameEndedPattern.Match(e.Data);
                    if (gameEndedMatch.Success == true) {
                        _Logger.LogDebug($"game ended, ignoring any errors past this [gameID={gameID}]");
                        gameEnded = true;
                    }

                    Match m = FramePattern.Match(e.Data);
                    if (m.Success == false) {
                        return;
                    }

                    if (m.Groups.Count < 2) {
                        _Logger.LogWarning($"expected more groups [gameID={gameID}] [groups.Count={m.Groups.Count}]");
                        return;
                    }

                    string frameStr = m.Groups[1].Value;
                    if (long.TryParse(frameStr, out long frame) == false) {
                        _Logger.LogWarning($"failed to parse frame string into a long [gameID={gameID}] [frame={frameStr}]");
                        return;
                    }

                    long replayTimer = timer.ElapsedMilliseconds - playbackStartedMs;
                    long deltaFrame = frame - previousFrame;
                    long deltaTimer = replayTimer - previousTimerUpdate;

                    HeadlessRunStatus status = new();
                    status.GameID = gameID;
                    status.Timestamp = DateTime.UtcNow;
                    status.Frame = frame;
                    status.DurationFrames = match.DurationFrameCount;
                    status.Simulating = frame > 0;
                    status.Fps = 0d;

                    if (frame == 0) {
                        _Logger.LogDebug($"game startup complete [gameID={gameID}] [timer={timer.ElapsedMilliseconds}ms] [engine={match.Engine}] [version={match.GameVersion}]");
                        playbackStartedMs = timer.ElapsedMilliseconds;
                        _Metric.RecordStartup(playbackStartedMs);
                    } else {
                        decimal fps = deltaFrame / (Math.Max(1, deltaTimer) / 1000m);
                        status.Fps = (double)fps;
                        decimal eta = (match.DurationFrameCount - frame) / Math.Max(0.01m, fps);
                        _Logger.LogDebug($"game frame update sent [gameID={gameID}] [frame={frame}/{match.DurationFrameCount}] [eta={eta:F1}s] "
                            + $"[timer={timer.ElapsedMilliseconds}ms] [speedup={fps / 30m:F3}] [fps={fps:F3}]");

                        DateTime timeEta = DateTime.UtcNow + TimeSpan.FromSeconds((long)eta);
                        if (timeEta > timeEnd) {
                            ++etaFailedCheck;
                            _Logger.LogWarning($"eta check failed [gameID={gameID}] [count={etaFailedCheck}] [eta={timeEta:u}] [end={timeEnd:u}]");

                            if (etaFailedCheck >= 10) {
                                _Logger.LogError($"this game will not complete before timeout occurs, killing the game [gameID={gameID}]");
                                increasePriority = true;
                                _HeadlessRunStatusRepository.Remove(gameID);
                                bar.Kill();
                            }
                        } else if (etaFailedCheck > 0) {
                            etaFailedCheck = 0;
                            _Logger.LogDebug($"eta check reset, game will finish in time [gameID={gameID}] [eta={timeEta:u}] [end={timeEnd:u}]");
                        }
                    }

                    _HeadlessRunStatusRepository.Upsert(gameID, status);
                    _HeadlessRunStatusQueue.Queue(status);

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
                        _Logger.LogError($"Gex Lua addon was not loaded! killing instance [gameID={gameID}] [error={e.Data}]");
                        _HeadlessRunStatusRepository.Remove(gameID);
                        bar.Kill();
                    }

                    if (e.Data.Contains(SOCKET_BIND_ERROR)) {
                        _Logger.LogError($"BAR failed to bind to port [gameID={gameID}] [port={port}] [error={e.Data}]");
                        _HeadlessRunStatusRepository.Remove(gameID);
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
                        _Logger.LogError($"Fatal error from stderr caught [gameID={gameID}] [msg={e.Data}]");
                    }
                }
            };

            bar.Start();

            bar.BeginOutputReadLine();
            bar.BeginErrorReadLine();

            // doing it this way prevents hangs due to not reading stdout or stderr
            // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            if (!(bar.WaitForExit(processingTimeout) && outputWaitHandle.WaitOne(processingTimeout) && errorWaitHandle.WaitOne(processingTimeout))) {
                increasePriority = true;
                _HeadlessRunStatusRepository.Remove(gameID);
                _Logger.LogWarning($"hit game processing timeout, increasing priority by 100 [gameID={gameID}] [timeout={processingTimeout}]");
            }

            if (increasePriority == true) {
                _HeadlessRunStatusRepository.Remove(gameID);

                // if a game takes too long to process, de-prio it for future runs, as it is taking too much time
                // away from other games that could be processed quicker
                BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
                if (proc != null) {
                    proc.Priority += 100;
                    await _ProcessingRepository.Upsert(proc);
                }

                return $"hit processing timeout for game [gameID={gameID}] [timeout={processingTimeout}]";
            }

            // game successfully ran, good job gex!
            _Logger.LogInformation($"BAR match ran locally [gameID={gameID}] [timer={timer.ElapsedMilliseconds}ms] "
                + $"[startup={playbackStartedMs}ms] [replay={timer.ElapsedMilliseconds - playbackStartedMs}ms]");

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
                _HeadlessRunStatusRepository.Remove(gameID);
                return $"failed to find action log after game ran! {actionLogLocation} was missing";
            }

            File.Copy(actionLogLocation, gameLogLocation + Path.DirectorySeparatorChar + "actions.json");
            _Logger.LogDebug($"game ran and output copied, deleting data dir [gameID={gameID}] [dataDir={dataDir}]");
            try {
                Directory.Delete(dataDir, true);
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to delete data dir after run! [gameID={gameID}] [dataDir={dataDir}] [ex={ex.Message}]");
            }

            try {
                await _HeadlessReplayHub.Clients.Group($"Gex.Headless.{gameID}").Finish();
                _HeadlessRunStatusRepository.Remove(gameID);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to send Finish event to {gameID}");
            }

            return new GameOutput() { GameID = gameID };
        }

    }
}
