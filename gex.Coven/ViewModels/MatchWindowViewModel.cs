using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.Constants;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models;
using gex.Coven.ViewModels.Match;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class MatchWindowViewModel : ViewModelBase {

        public MatchWindowViewModel() {

        }

        public MatchWindowViewModel(BarMatch match) {
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

    }
}
