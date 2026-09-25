using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Code.Constants;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Event;
using gex.Common.Models.Match;
using gex.Common.Services.Bar;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models;
using gex.Coven.Models.Config;
using gex.Coven.Models.Match;
using gex.Coven.Services;
using gex.Coven.Services.Bar;
using gex.Coven.Services.Util;
using gex.Coven.ViewModels.Match;
using gex.Coven.Windows;
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
        private readonly ActionLogParser _ActionLogParser = default!;
        private readonly StorageUtil _StorageUtil = default!;

        public MatchWindowViewModel() {

        }

        public MatchWindowViewModel(BarMatch match) {
            _Logger = App.Current?.Services?.GetService<ILogger<MatchWindowViewModel>>() ?? default!;
            _ActionLogParser = App.Current?.Services.GetService<ActionLogParser>() ?? default!;
            _StorageUtil = App.Current?.Services.GetService<StorageUtil>() ?? default!;

            _Match = new BarMatchViewModel(match);
            if (_StorageUtil.HasActionLog(match.ID) == true) {
                try {
                    Result<string, string> actionLog = Task.Run(() => _StorageUtil.GetActionLog(match.ID, CancellationToken.None))
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    if (actionLog.IsOk == true) {
                        Result<GameOutput, string> output = _ActionLogParser.Parse(match.ID, actionLog.Value, CancellationToken.None);
                        if (output.IsOk == true) {
                            Output = output.Value;
                        } else {
                            _Logger.LogError($"failed to parse action log [gameID={match.ID}] [error={output.Error}]");
                        }
                    } else {
                        _Logger.LogError($"failed to load action log from storage [gameID={match.ID}] [error={actionLog.Error}]");
                    }
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"exception loading action log");
                }
            }

            Entities = BarMatchEntity.GetEntities(match);

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

            _Milestones = new BarMatchMilestonesViewModel(this);

            _ChatMessagesViewModel = new BarMatchChatMessagesViewModel(match);
            _TeamStatsViewModel = new BarMatchViewTeamStats(this);
        }

        [ObservableProperty]
        private BarMatchViewModel _Match = new();

        public GameOutput? Output { get; private set; } = null;

        public List<BarMatchEntity> Entities { get; private set; } = [];

        [ObservableProperty]
        private string _Title = "gex.Coven";

        [ObservableProperty]
        private BarMatchViewTeamStats _TeamStatsViewModel = new();

        [ObservableProperty]
        private BarMatchChatMessagesViewModel _ChatMessagesViewModel = new();

        [ObservableProperty]
        private BarMatchMilestonesViewModel _Milestones = new();

        [RelayCommand]
        public void LaunchReplay() {
            DemofileLaunchReplayWindow win = new() {
                DataContext = new DemofileLaunchReplayViewModel() { }
            };

            WindowManager.Register(win);
            win.Show();
        }

    }
}
