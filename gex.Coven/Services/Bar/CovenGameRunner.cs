using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Bar;
using gex.Coven.Models.Config;
using gex.Coven.Services.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Bar {

    public class CovenGameRunner {

        /// <summary>
        ///     regex to find the messages from the widget that tell Gex what from the replay is on
        /// </summary>
        private static Regex FramePattern = new(@"^\[t=.*?\]\[f=\d*?\] \[Gex\] on frame (\d+?)$");

        private static Regex LoadScreenSetLoadMessagePattern = new(@"^\[t=.*?\]\[f=(?:-|\d)*?\] \[LoadScreen::SetLoadMessage\] text=""(.*)""$");

        private readonly ILogger<CovenGameRunner> _Logger;
        private readonly IPrDownloaderService _PrDownloader;
        private readonly IBarEngineDownloader _EngineDownloader;
        private readonly UserOptionsService _UserOptions;
        private readonly StorageUtil _StorageUtil;

        public CovenGameRunner(ILogger<CovenGameRunner> logger,
            IPrDownloaderService prDownloader, IBarEngineDownloader engineDownloader,
            UserOptionsService userOptions, StorageUtil storageUtil) {

            _Logger = logger;
            _PrDownloader = prDownloader;
            _EngineDownloader = engineDownloader;
            _UserOptions = userOptions;
            _StorageUtil = storageUtil;
        }

        public async Task LaunchReplay(BarMatch match, bool headless, CancellationToken cancel) {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64) {
                _Logger.LogWarning($"cannot replay demofile, system architecure is not amd64 [arch={RuntimeInformation.ProcessArchitecture}]");
                return;
            }

            try {
                _Logger.LogDebug($"launching replay [gameID={match.ID}]");

                UserOptions userOptions = _UserOptions.Load();
                string gameID = match.ID;

                // make sure the demofile exists
                string demofileLocation = Path.Join(userOptions.InstallFolder, "demos", match.FileName);
                if (File.Exists(demofileLocation) == false) {
                    _Logger.LogInformation($"cannot find demofile [demofileLocation={demofileLocation}]");
                    return;
                }

                if (_EngineDownloader.HasEngine(match.Engine) == false) {
                    _Logger.LogDebug($"missing engine, downloading [engine={match.Engine}]");
                    Stopwatch dlTimer = Stopwatch.StartNew();
                    await _EngineDownloader.DownloadEngine(match.Engine, cancel);
                    _Logger.LogDebug($"downloaded engine [engine={match.Engine}] [timer={dlTimer.ElapsedMilliseconds}ms]");
                }

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
                    return;
                }

                // ensure map is downloaded
                if (_PrDownloader.HasMap(match.Engine, match.Map) == false) {
                    _Logger.LogDebug($"missing map, fetching [gameID={gameID}] [engine={match.Engine}] [map={match.Map}]");
                    await _PrDownloader.GetMap(match.Engine, match.Map, cancel);
                }

                if (_PrDownloader.HasMap(match.Engine, match.Map) == false) {
                    _Logger.LogError($"failed to fetch map [gameID={gameID}] [engine={match.Engine}] [map={match.Map}]");
                    return;
                }

                string mapName = (match.Map + ".sd7").EscapeRecoilFilesytemCharacters().ToLower();
                string mapPath = Path.Join(userOptions.InstallFolder, "maps", mapName);
                if (File.Exists(mapPath) == false) {
                    _Logger.LogError($"missing map from install folder [map={match.Map}] [mapPath={mapPath}]");
                    return;
                }

                string enginePath = Path.Join(userOptions.InstallFolder, "engine", match.Engine);
                string engineMapPath = Path.Join(enginePath, "maps", mapName);
                if (File.Exists(engineMapPath) == false) {
                    File.Copy(mapPath, Path.Join(enginePath, "maps", mapName));
                }

                if (headless == true) {
                    // actually run the process now that everything is setup
                    Process bar = new();
                    bar.StartInfo.FileName = Path.Join(enginePath, "spring-headless");
                    if (OperatingSystem.IsWindows()) { bar.StartInfo.FileName += ".exe"; }
                    bar.StartInfo.WorkingDirectory = enginePath;
                    bar.StartInfo.Arguments = $"--write-dir \"{enginePath}\" \"{demofileLocation}\"";
                    bar.StartInfo.UseShellExecute = false;
                    bar.StartInfo.RedirectStandardOutput = true;
                    bar.StartInfo.RedirectStandardError = true;
                    bar.StartInfo.CreateNoWindow = true;

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    HeadlessRunStatus status = new();
                    status.GameID = gameID;
                    status.Timestamp = DateTime.UtcNow;
                    status.Frame = 0;
                    status.DurationFrames = match.DurationFrameCount;
                    status.Simulating = false;
                    status.Fps = 0d;
                    HeadlessProgressUpdate?.Invoke(this, status);

                    long previousTimerUpdate = 0;
                    long previousFrame = 0;

                    Stopwatch timer = Stopwatch.StartNew();
                    long playbackStartedMs = 0;
                    DateTime playbackStarted = DateTime.UtcNow;

                    TimeSpan timeout = TimeSpan.FromHours(2);

                    bool gameEnded = false;
                    //using AutoResetEvent outputWaitHandle = new(false);
                    //using AutoResetEvent errorWaitHandle = new(false);
                    bar.OutputDataReceived += (sender, e) => {
                        if (e.Data == null) {
                            try {
                                //outputWaitHandle.Set();
                            } catch (Exception ex) {
                                _Logger.LogError(ex, $"failed to capture stdout");
                            }
                        } else {
                            output.AppendLine(e.Data);
                            _Logger.LogTrace(e.Data);
                            HeadlessStdoutLine?.Invoke(this, gameID, e.Data);

                            HeadlessRunStatus status = new();
                            status.GameID = gameID;
                            status.Timestamp = DateTime.UtcNow;

                            Match loadScreenMsg = LoadScreenSetLoadMessagePattern.Match(e.Data);
                            if (loadScreenMsg.Success == true) {
                                if (loadScreenMsg.Groups.Count < 2) {
                                    _Logger.LogWarning($"expected more groups for SetLoadMessage [gameID={gameID}] [groups.Count={loadScreenMsg.Groups.Count}]");
                                    return;
                                }

                                string text = loadScreenMsg.Groups[1].Value;
                                status.LoadingStep = text;
                            } else {
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

                                status.Frame = frame;
                                status.DurationFrames = match.DurationFrameCount;
                                status.Simulating = frame > 0;
                                status.Fps = 0d;

                                if (frame == 0) {
                                    _Logger.LogDebug($"game startup complete [gameID={gameID}] [timer={timer.ElapsedMilliseconds}ms]"
                                        + $" [engine={match.Engine}] [version={match.GameVersion}]");
                                    playbackStartedMs = timer.ElapsedMilliseconds;
                                    status.LoadingStep = "";
                                } else {
                                    decimal fps = deltaFrame / (Math.Max(1, deltaTimer) / 1000m);
                                    status.Fps = (double)fps;
                                    decimal eta = (match.DurationFrameCount - frame) / Math.Max(0.01m, fps);
                                    _Logger.LogDebug($"game frame update sent [gameID={gameID}] [frame={frame}/{match.DurationFrameCount}] [eta={eta:F1}s] "
                                        + $"[timer={timer.ElapsedMilliseconds}ms] [speedup={fps / 30m:F3}] [fps={fps:F3}]");

                                    DateTime timeEta = DateTime.UtcNow + TimeSpan.FromSeconds((long)eta);
                                }

                                previousTimerUpdate = timer.ElapsedMilliseconds - playbackStartedMs;
                                previousFrame = frame;
                            }

                            HeadlessProgressUpdate?.Invoke(this, status);
                        }
                    };
                    bar.ErrorDataReceived += (sender, e) => {
                        if (e.Data == null) {
                            try {
                                //errorWaitHandle.Set();
                            } catch (Exception ex) {
                                _Logger.LogError(ex, $"failed to capture stdout");
                            }
                        } else {
                            error.AppendLine(e.Data);

                            if (e.Data.Contains("Failed to load: gex.lua")) {
                                _Logger.LogError($"Gex Lua addon was not loaded! killing instance [gameID={gameID}] [error={e.Data}]");
                                bar.Kill();
                            }

                            if (gameEnded == true) {
                                return;
                            }

                            HeadlessStderrLine?.Invoke(this, gameID, e.Data);

                            _Logger.LogError(e.Data);
                        }
                    };

                    _Logger.LogInformation($"starting bar executable [gameID={gameID}] "
                        + $"[cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}]");

                    _ = Task.Run(async () => {
                        try {
                            bar.Start();

                            bar.BeginOutputReadLine();
                            bar.BeginErrorReadLine();

                            await bar.WaitForExitAsync();

                            _Logger.LogInformation($"game processed [gameID={gameID}]");
                        } catch (Exception ex) {
                            _Logger.LogError(ex, $"exception running BAR process");
                        } finally {
                            bar.Dispose();
                        }

                        string actionsJson = Path.Join(userOptions.InstallFolder, "engine", match.Engine, "actions.json");

                        if (File.Exists(actionsJson) == false) {
                            _Logger.LogError($"missing actions.json after replaying a game [gameID={match.ID}] [path={actionsJson}]");
                            return;
                        }

                        string contents = File.ReadAllText(actionsJson);
                        await _StorageUtil.SaveActionLog(gameID, contents, CancellationToken.None);
                        _Logger.LogInformation($"saved action log [gameID={gameID}]");
                        HeadlessDone?.Invoke(this, gameID);
                    }, cancel);
                } else {
                    // actually run the process now that everything is setup
                    using Process bar = new();
                    bar.StartInfo.FileName = Path.Join(enginePath, "spring");
                    if (OperatingSystem.IsWindows()) { bar.StartInfo.FileName += ".exe"; }
                    bar.StartInfo.WorkingDirectory = enginePath;

                    string writeDir = userOptions.InstallFolder;
                    if (headless == true) {
                        writeDir = enginePath;
                    }

                    bar.StartInfo.Arguments = $"--write-dir \"{writeDir}\" \"{demofileLocation}\"";
                    bar.StartInfo.UseShellExecute = false;
                    bar.StartInfo.RedirectStandardOutput = false;
                    bar.StartInfo.RedirectStandardError = false;

                    // this callback is Dispose-able, so even tho we don't use this, we still want to capture it for Disposale
                    using CancellationTokenRegistration cancelCallback = cancel.Register(() => {
                        _Logger.LogInformation($"killing BAR instance due to cancellation [gameID={gameID}]");
                        bar.Kill();
                    });

                    Stopwatch timer = Stopwatch.StartNew();

                    _Logger.LogInformation($"starting bar executable [gameID={gameID}] "
                        + $"[cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}]");

                    bar.Start();
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to launch replay");
            }
        }

        public delegate void HeadlessProgressUpdateHandler(object sender, HeadlessRunStatus status);
        public event HeadlessProgressUpdateHandler? HeadlessProgressUpdate;

        public delegate void HeadlessDoneHandler(object sender, string gameID);
        public event HeadlessDoneHandler? HeadlessDone;

        public delegate void HeadlessStdoutLineHandler(object sender, string gameID, string line);
        public event HeadlessStdoutLineHandler? HeadlessStdoutLine;

        public delegate void HeadlessStderrLineHandler(object sender, string gameID, string line);
        public event HeadlessStderrLineHandler? HeadlessStderrLine;

    }
}
