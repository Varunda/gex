using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Code.Constants;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Bar;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.ViewModels.Match;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class MatchWindowViewModel : ViewModelBase {

        private readonly ILogger<MatchWindowViewModel> _Logger = default!;

        public MatchWindowViewModel() {

        }

        public MatchWindowViewModel(BarMatch match) {
            _Logger = App.Current?.Services?.GetService<ILogger<MatchWindowViewModel>>() ?? default!;

            _Match = new BarMatchViewModel(match);

            _Title = $"{_Match.StartTime:yyyy-MM-dd} | {_Match.Map}: ";

            if (_Match.GamemodeID == BarGamemode.DUEL) {
                if (_Match.AllyTeams.Count < 2) {
                    _Title += $"expected 2 teams for a duel";
                } else {
                    BarMatchTeamViewModel? team0 = _Match.AllyTeams[0].Teams.FirstOrDefault();
                    BarMatchTeamViewModel? team1 = _Match.AllyTeams[1].Teams.FirstOrDefault();

                    _Title += $"{team0?.Name} v {team1?.Name}";
                }
            } else if (_Match.GamemodeID == BarGamemode.FFA) {
                _Title += $"{_Match.AllyTeams.Count}-way FFA";
            } else {
                _Title += $"{string.Join(" v ", _Match.AllyTeams.Select(iter => iter.TeamCount))}";
            }

            _AddTeamStats(match, "Damage dealt", iter => (decimal)iter.DamageDealt);
            _AddTeamStats(match, "Damage taken", iter => (decimal)iter.DamageReceived);
            _AddTeamStats(match, "Energy excessed", iter => (decimal)iter.EnergyExcess);
            _AddTeamStats(match, "Energy produced", iter => (decimal)iter.EnergyProduced);
            _AddTeamStats(match, "Energy received", iter => (decimal)iter.EnergyReceived);
            _AddTeamStats(match, "Energy sent", iter => (decimal)iter.EnergySend);
            _AddTeamStats(match, "Energy used", iter => (decimal)iter.EnergyUsed);
            _AddTeamStats(match, "Metal excessed", iter => (decimal)iter.MetalExcess);
            _AddTeamStats(match, "Metal produced", iter => (decimal)iter.MetalProduced);
            _AddTeamStats(match, "Metal received", iter => (decimal)iter.MetalReceived);
            _AddTeamStats(match, "Metal sent", iter => (decimal)iter.MetalSend);
            _AddTeamStats(match, "Metal used", iter => (decimal)iter.MetalUsed);
            _AddTeamStats(match, "Units captured", iter => (decimal)iter.UnitsCaptured);
            _AddTeamStats(match, "Units died", iter => (decimal)iter.UnitsDied);
            _AddTeamStats(match, "Units killed", iter => (decimal)iter.UnitsKilled);
            _AddTeamStats(match, "Units lost to capture", iter => (decimal)iter.UnitsOutCaptured);
            _AddTeamStats(match, "Units made", iter => (decimal)iter.UnitsProduced);
            _AddTeamStats(match, "Units received", iter => (decimal)iter.UnitsReceived);
            _AddTeamStats(match, "Units sent", iter => (decimal)iter.UnitsSent);

            _TeamStatKeys = new ObservableCollection<string>(_TeamStats.Keys);
            _SelectedTeamStatKey = _TeamStatKeys[0];
            _SelectedTeamStat = _TeamStats.GetValueOrDefault(_SelectedTeamStatKey)!;

            _ChatMessagesViewModel = new BarMatchChatMessagesViewModel(match);
        }

        [ObservableProperty]
        private BarMatchViewModel _Match = new();

        [ObservableProperty]
        private string _Title = "gex.Coven";

        #region Team stats

        /// <summary>
        ///     collection of all the team stats
        /// </summary>
        [ObservableProperty]
        private AvaloniaDictionary<string, ChartSeriesCollection> _TeamStats = new();

        /// <summary>
        ///     the keys of all the team stats, what's selectable to show
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _TeamStatKeys = new();

        /// <summary>
        ///     the chart series that are the selected team stats being shown
        /// </summary>
        [ObservableProperty]
        private ChartSeriesCollection _SelectedTeamStat = new();

        /// <summary>
        ///     the name of the key in _TeamStats that is selected (and has all the team stats)
        /// </summary>
        [ObservableProperty]
        private string _SelectedTeamStatKey = "";

        #endregion

        [ObservableProperty]
        private BarMatchChatMessagesViewModel _ChatMessagesViewModel = new();

        /// <summary>
        ///     select a team stats to show
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SelectTeamStatsKey(string key) {
            if (TeamStatKeys.Contains(key) == false) {
                return;
            }

            ChartSeriesCollection series = TeamStats.GetValueOrDefault(key)
                ?? throw new InvalidOperationException($"missing expected TeamStats value [key={key}]");

            SelectedTeamStatKey = key;
            SelectedTeamStat = series;
        }

        /// <summary>
        ///     create a new chart series 
        /// </summary>
        /// <param name="match"></param>
        /// <param name="name"></param>
        /// <param name="selector"></param>
        private void _AddTeamStats(BarMatch match, string name, Func<BarMatchTeamStats, decimal> selector) {
            ChartSeriesCollection coll = new();
            coll.Labels = match.TeamStats.Select(iter => iter.Frame).Distinct().Order().Select(iter => {
                return TimeSpan.FromSeconds(iter / 30d).GetRelativeFormat();
            }).ToList();

            foreach (BarMatchAllyTeamViewModel allyTeam in Match.AllyTeams) {

                foreach (BarMatchTeamViewModel team in allyTeam.Teams) {
                    List<BarMatchTeamStats> ts = match.TeamStats.Where(iter => iter.TeamID == team.TeamID).OrderBy(iter => iter.Frame).ToList();

                    ChartSeries cs = new ChartSeries() {
                        Name = team.Name,
                        Values = [.. ts.Select(selector)],
                        Color = SolidColorPaint.Parse(team.HexColor)!,
                    };

                    cs.Color.StrokeThickness = 2;

                    coll.Series.Add(cs);
                }
            }

            TeamStats.Add(name, coll);
        }

        [RelayCommand]
        public async Task LaunchReplay() {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64) {
                _Logger.LogWarning($"cannot replay demofile, system architecure is not amd64 [arch={RuntimeInformation.ProcessArchitecture}]");
                return;
            }

            try {
                IPrDownloaderService _PrDownloader = App.Current.Services.GetRequiredService<IPrDownloaderService>();
                IBarEngineDownloader _EngineDownloader = App.Current.Services.GetRequiredService<IBarEngineDownloader>();
                UserOptionsService _UserOptions = App.Current.Services.GetRequiredService<UserOptionsService>();

                _Logger.LogDebug($"launching replay [gameID={Match.GameID}]");

                UserOptions userOptions = _UserOptions.Load();
                BarMatch match = Match.Match;
                string gameID = match.ID;
                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
                CancellationToken cancel = cts.Token;

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

                string enginePath = Path.Join(userOptions.InstallFolder, "engine", match.Engine);

                //await File.WriteAllTextAsync(scriptsFile, $"[game] {{\ndemofile={demofileLocation};\n}}", cancel);

                // actually run the process now that everything is setup
                using Process bar = new();
                bar.StartInfo.FileName = Path.Join(enginePath, "spring");
                if (OperatingSystem.IsWindows()) { bar.StartInfo.FileName += ".exe"; }
                bar.StartInfo.WorkingDirectory = enginePath;
                bar.StartInfo.Arguments = $"--write-dir \"{userOptions.InstallFolder}\" \"{demofileLocation}\"";
                bar.StartInfo.UseShellExecute = false;
                bar.StartInfo.RedirectStandardOutput = false;
                bar.StartInfo.RedirectStandardError = false;

                // this callback is Dispose-able, so even tho we don't use this, we still want to capture it for Disposale
                using CancellationTokenRegistration cancelCallback = cancel.Register(() => {
                    _Logger.LogInformation($"killing BAR instance due to cancellation [gameID={gameID}]");
                    bar.Kill();
                });

                Stopwatch timer = Stopwatch.StartNew();

                _Logger.LogDebug($"starting bar executable [gameID={gameID}] [cwd={bar.StartInfo.WorkingDirectory}] [args={bar.StartInfo.Arguments}]");

                bar.Start();
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to launch replay");
            }
        }

    }
}
