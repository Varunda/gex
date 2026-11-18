using Avalonia.Platform.Storage;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Common.Code.ExtensionMethods;
using gex.Coven.Models.Config;
using gex.Coven.Models.Match;
using gex.Coven.Services.Db.Match;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Timers;

namespace gex.Coven.Services {

    public class DemofileWatcher {

        private readonly ILogger<DemofileWatcher> _Logger;
        private readonly BarDemofileParser _DemofileParser;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchHashDb _MatchHashDb;
        private readonly UserOptionsService _UserOptionsService;
        private readonly StartSpotDataParser _StartSpotDataParser;
        private readonly BarMatchIgnoredFilesDb _IgnoredFilesDb;

        private FileSystemWatcher? _FileWatcher;
        private System.Timers.Timer? _WriteTimer;

        private static Dictionary<string, DateTime> _WriteTimeout = [];
        private string? _PreviousReplayFolder = null;

        public DemofileWatcher(ILogger<DemofileWatcher> logger,
            BarDemofileParser demofileParser, BarMatchRepository matchRepository,
            BarMatchHashDb matchHashDb, UserOptionsService userOptionsService,
            StartSpotDataParser startSpotDataParser, BarMatchIgnoredFilesDb ignoredFilesDb) {

            _Logger = logger;
            _UserOptionsService = userOptionsService;
            _UserOptionsService.OptionsUpdated += _UserOptionsService_OptionsUpdated;
            _DemofileParser = demofileParser;
            _MatchRepository = matchRepository;
            _MatchHashDb = matchHashDb;
            _StartSpotDataParser = startSpotDataParser;
            _IgnoredFilesDb = ignoredFilesDb;

            _WriteTimer = new(TimeSpan.FromSeconds(1));
            _WriteTimer.Elapsed += _WriteTimer_Callback;
            _WriteTimer.AutoReset = true;
            _WriteTimer.Enabled = true;

            _UserOptionsService_OptionsUpdated(this, _UserOptionsService.Load());
        }

        private void _UserOptionsService_OptionsUpdated(object sender, UserOptions options) {
            string replayFolder = Path.Join(options.InstallFolder, "demos");
            if (string.IsNullOrWhiteSpace(replayFolder) || Directory.Exists(replayFolder) == false) {
                _Logger.LogError($"ReplayFolder is null/empty, or points to a directory that doesn't exist");
                _FileWatcher ??= new FileSystemWatcher(Environment.CurrentDirectory);
                return;
            }

            if (replayFolder != _PreviousReplayFolder) {
                _PreviousReplayFolder = replayFolder;
                _Logger.LogInformation($"replay folder changed [replayFolder={replayFolder}]");
                CreateFileWatcher(replayFolder);
            }
        }

        private void _WriteTimer_Callback(object? sender, ElapsedEventArgs args) {
            List<string> toProcess = [];
            lock (_WriteTimeout) {
                foreach (KeyValuePair<string, DateTime> entry in _WriteTimeout) {
                    if (entry.Value < DateTime.UtcNow) {
                        toProcess.Add(entry.Key);
                    }
                }

                foreach (string key in toProcess) {
                    _WriteTimeout.Remove(key);
                }
            }

            foreach (string key in toProcess) {
                _Logger.LogInformation($"write timeout finished, processing match [path={key}]");

                try {
                    Task.Run(async () => {
                        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                        await _ProcessMatch(key, cts.Token);
                    });
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to process match in timeout [key={key}]");
                }
            }
        }

        /// <summary>
        ///     create a new file watcher and load all matches from the replay folder
        /// </summary>
        /// <param name="replayFolder">path to the folder that contains the replays</param>
        private void CreateFileWatcher(string replayFolder) {
            if (_FileWatcher != null) {
                _FileWatcher.Changed -= FileWatcher_OnWrite;
                _FileWatcher.Dispose();
                _FileWatcher = null;
            }

            _FileWatcher = new FileSystemWatcher(replayFolder);
            _FileWatcher.Filter = "*.sdfz";
            _FileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _FileWatcher.EnableRaisingEvents = true;
            _FileWatcher.Changed += FileWatcher_OnWrite;

            Task.Run(async () => {
                try {
                    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(10));
                    await LoadAll(cts.Token);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to load all");
                }
            });
        }

        public delegate void NewMatchReadyHandler(object sender, BarMatch match);
        public event NewMatchReadyHandler? NewMatchReady;

        private async void FileWatcher_OnWrite(object sender, FileSystemEventArgs args) {
            if (args.ChangeType != WatcherChangeTypes.Changed) {
                return;
            }

            FileInfo fi = new(args.FullPath);
            if (fi.Length == 0) {
                _Logger.LogInformation($"file is 0 size [file={args.FullPath}]");
                return;
            }

            _Logger.LogInformation($"new file [file={args.FullPath}]");

            if (args.Name == null) {
                Debug.Fail("why is args.Name null");
            }

            // instead of immediately parsing the processing the game, wait 2 seconds for any future writes
            // this is useful when downloading a replay
            lock (_WriteTimeout) {
                _WriteTimeout[args.FullPath] = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                _Logger.LogDebug($"write timeout updated for file [path={args.FullPath}] [timeout={_WriteTimeout[args.FullPath]:u}]");
            }
        }

        public async Task LoadAll(CancellationToken cancel) {
            if (_FileWatcher == null) {
                _Logger.LogWarning($"FileWatcher is null, cannot LoadAll");
                return;
            }

            string[] demofiles = Directory.GetFiles(_FileWatcher.Path, "*.sdfz").OrderDescending().ToArray();
            _Logger.LogInformation($"found demofiles [count={demofiles.Length}]");

            List<BarMatchIgnoredFile> ignored = await _IgnoredFilesDb.GetAll(cancel);
            HashSet<string> ignoredNames = [ ..ignored.Select(iter => iter.FileName) ];

            foreach (string demofile in demofiles) {
                FileInfo fi = new(demofile);
                if (fi.Length == 0) {
                    continue;
                }

                string filename = Path.GetFileName(demofile);
                if (ignoredNames.Contains(filename) == true) {
                    continue;
                }

                byte[] bytes = await File.ReadAllBytesAsync(demofile, cancel);

                string md5 = string.Join("", MD5.HashData(bytes).Select(iter => iter.ToString("x2"))).ToLower();
                BarMatchHash? existingHash = await _MatchHashDb.GetByHash(md5, CancellationToken.None);
                if (existingHash != null) {
                    // ensure the match is in the db, not just the hash
                    BarMatch? match = await _MatchRepository.GetByID(existingHash.GameID, CancellationToken.None);
                    if (match != null) {
                        continue;
                    }
                }

                _Logger.LogInformation($"loading new match [file={demofile}] [hash={md5}]");

                try {
                    Result<BarMatch, string> ret = await _DemofileParser.Parse(filename, bytes, new DemofileParserOptions() {

                    }, cancel);

                    if (ret.IsOk == true) {
                        BarMatch match = ret.Value;

                        BarMatch? existingMatchByID = await _MatchRepository.GetByID(match.ID, cancel);
                        if (existingMatchByID != null) {
                            _Logger.LogDebug($"duplicate match by ID found, ignoring [gameID={match.ID}] [filename={filename}]");
                            await _IgnoredFilesDb.Insert(new BarMatchIgnoredFile() {
                                FileName = filename,
                                Reason = $"duplicate match ID {match.ID}",
                            }, cancel);
                            continue;
                        }

                        StartSpotData? startSpotData = null;
                        JsonElement? matchStartPos = match.GameSettings.GetChild("mapmetadata_startpos");
                        if (matchStartPos != null && matchStartPos.Value.ValueKind != JsonValueKind.Undefined && matchStartPos.Value.GetString() != "") {
                            Result<StartSpotData, string> result = await _StartSpotDataParser.ParseB64(ret.Value.MapName, matchStartPos.Value.GetString()!, cancel);

                            do {
                                if (result.IsOk == false) {
                                    _Logger.LogError($"failed to parse start spot data [map={match.MapName}] [gameID={match.ID}] [error={result.Error}]");
                                    break;
                                }

                                (bool valid, int found, int missing) = _CheckStartSpotValidity(match, result.Value);
                                if (valid == false) {
                                    _Logger.LogWarning($"ignoring mapmetadata_startpos from match as validity check failed "
                                        + $"[map={match.MapName}] [gameID={match.ID}] [found={found}] [missing={missing}]");
                                    break;
                                }

                                startSpotData = result.Value;
                            } while (false);
                        }

                        if (startSpotData != null) {
                            foreach (BarMatchTeam team in match.Teams) {
                                StartSpotSideStart? startSpot = startSpotData.GetNearestStartSpot(match.AllyTeams.Count, team.StartingPosition.X, team.StartingPosition.Z);

                                team.StartSpot = startSpot?.SpawnPoint;
                                team.StartSpotLabel = startSpot?.Role;
                            }
                        }

                        await _MatchHashDb.Upsert(new BarMatchHash() {
                            GameID = ret.Value.ID,
                            FileName = filename,
                            Hash = md5
                        }, cancel);

                        _Logger.LogInformation($"loaded new match [gameID={ret.Value.ID}]");
                        NewMatchReady?.Invoke(this, ret.Value);
                    } else {
                        _Logger.LogWarning($"failed to parse demofile [filename={filename}] [error={ret.Error}]");
                        await _IgnoredFilesDb.Insert(new BarMatchIgnoredFile() {
                            FileName = filename,
                            Reason = $"failed to parse: {ret.Error}",
                        }, cancel);
                    }
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to parse demofile [path={demofile}]");
                }
            }
        }

        private async Task _ProcessMatch(string path, CancellationToken cancel) {
            try {
                byte[] bytes = await File.ReadAllBytesAsync(path, cancel);
                Result<BarMatch, string> ret = await _DemofileParser.Parse(Path.GetFileName(path), bytes, new DemofileParserOptions() {}, cancel);

                if (ret.IsOk == false) {
                    _Logger.LogError($"failed to parse demofile [path={path}] [error={ret.Error}]");
                    return;
                }

                _Logger.LogInformation($"found new match [path={path}] [gameID={ret.Value.ID}]");
                NewMatchReady?.Invoke(this, ret.Value);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse demofile [path={path}]");
            }
        }

        /// <summary>
        ///     check if a start spot is valid for a specific match, where at least half of the players in the match
        ///     have a start spot position found from the start spot data
        /// </summary>
        /// <param name="match">match that contains the players to check the positions of</param>
        /// <param name="data">start spot data that contains the positions to ensure</param>
        /// <returns>
        ///     a tuple containing a bool and 2 ints. the bool indicates if the start spot data is valid for that
        ///     particular match, while the ints provide how many start spots were found to be valid, and how 
        ///     many were missing a valid start spot
        /// </returns>
        private (bool valid, int found, int missing) _CheckStartSpotValidity(BarMatch match, StartSpotData data) {
            int foundSpot = 0;
            int missingSpot = 0;
            foreach (BarMatchTeam team in match.Teams) {
                StartSpotSideStart? startSpot = data.GetNearestStartSpot(match.AllyTeams.Count, team.StartingPosition.X, team.StartingPosition.Z);
                if (startSpot != null) {
                    ++foundSpot;
                } else {
                    ++missingSpot;
                }
            }

            int playerCount = match.Players.Count;
            return (playerCount / 2 < foundSpot, foundSpot, missingSpot);
        }

    }
}
