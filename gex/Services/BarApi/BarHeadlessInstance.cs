﻿using gex.Models;
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
        private readonly EnginePathUtil _EnginePathUtil;

        private static int _PortOffset = 0;

		private static Regex FramePattern = new(@"^\[t=.*?\]\[f=\d*?\] \[Gex\] on frame (\d+?)$");

		public BarHeadlessInstance(ILogger<BarHeadlessInstance> logger,
			IOptions<FileStorageOptions> options, BarMatchRepository matchRepository,
			PrDownloaderService prDownloader, BarEngineDownloader engineDownloader,
            EnginePathUtil enginePathUtil) {

			_Logger = logger;
			_Options = options;
			_MatchRepository = matchRepository;
			_PrDownloader = prDownloader;
			_EngineDownloader = engineDownloader;
			_EnginePathUtil = enginePathUtil;
		}

		public async Task<Result<GameOutput, string>> RunGame(string gameID, bool force, CancellationToken cancel) {

            _Logger.LogDebug($"starting BAR headless instance [gameID={gameID}] [cwd={Environment.CurrentDirectory}]");

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return $"cannot find game with ID '{gameID}'";
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

            _Logger.LogDebug($"starting bar executable [gameID={gameID}] [port={port}] "
                + $"[cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}] [timeout={processingTimeout}]");

            using AutoResetEvent outputWaitHandle = new(false);
            using AutoResetEvent errorWaitHandle = new(false);
            bar.OutputDataReceived += (sender, e) => {
                if (e.Data == null) {
                    outputWaitHandle.Set();
                } else {
                    output.AppendLine(e.Data);

					if (e.Data.Contains("Failed to load: gex.lua")) {
						_Logger.LogError($"Gex Lua addon was not loaded! killing instance [gameID={gameID}] [error={e.Data}]");
						bar.Kill();
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

					if (frame == 0) {
						_Logger.LogDebug($"game startup complete [gameID={gameID}] [timer={timer.ElapsedMilliseconds}ms] [engine={match.Engine}] [version={match.GameVersion}]");
						playbackStartedMs = timer.ElapsedMilliseconds;
					} else {
						decimal fps = (decimal)frame / (timer.ElapsedMilliseconds - playbackStartedMs) * 1000m;
						decimal speedup = (frame / 30m) / (timer.ElapsedMilliseconds - playbackStartedMs) * 1000m;
						decimal eta = (match.DurationFrameCount - frame) / Math.Max(0.01m, fps);
						_Logger.LogDebug($"game frame progressing [gameID={gameID}] [frame={frame}/{match.DurationFrameCount}] [eta={eta:F1}s] "
							+ $"[timer={timer.ElapsedMilliseconds}ms] [speedup={speedup:F3}] [fps={fps:F3}]");
					}
                }
            };
            bar.ErrorDataReceived += (sender, e) => {
                if (e.Data == null) {
                    errorWaitHandle.Set();
                } else {
                    error.AppendLine(e.Data);

					if (e.Data.Contains("Failed to load: gex.lua")) {
						_Logger.LogError($"Gex Lua addon was not loaded! killing instance [gameID={gameID}] [error={e.Data}]");
						bar.Kill();
					}
                }
            };

            bar.Start();

            bar.BeginOutputReadLine();
            bar.BeginErrorReadLine();

            // doing it this way prevents hangs due to not reading stdout or stderr
            // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            if (!(bar.WaitForExit(processingTimeout) && outputWaitHandle.WaitOne(processingTimeout) && errorWaitHandle.WaitOne(processingTimeout))) {
                _Logger.LogWarning($"hit game processing timeout [gameID={gameID}] [timeout={processingTimeout}]");
                return "took longer to process the game than the game ran, something went wrong!";
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

    }
}
