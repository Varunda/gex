using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Event;
using gex.Common.Models.Match;
using gex.Coven.Code;
using gex.Coven.Models.Chart;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchViewTeamStats : ViewModelBase {

        private readonly ILogger<MatchWindowViewModel> _Logger = default!;

        public BarMatchViewTeamStats() {

        }

        public BarMatchViewTeamStats(BarMatch match, GameOutput? output) {
            _Logger = App.Current?.Services?.GetService<ILogger<MatchWindowViewModel>>() ?? default!;

            Match = new BarMatchViewModel(match);
            Output = output;

            _AddTeamStats(match, "Damage dealt", iter => (decimal)iter.DamageDealt);
            _AddTeamStats(match, "Damage taken", iter => (decimal)iter.DamageReceived);

            _AddTeamStats(match, "Energy excess", iter => (decimal)iter.EnergyExcess);
            _AddTeamStats(match, "Energy excess %",
                iter => (decimal)(iter.EnergyExcess / Math.Max(1d, iter.EnergyProduced)) * 100m);
            _AddTeamStats(match, "Energy produced", iter => (decimal)iter.EnergyProduced);
            _AddTeamStats(match, "Energy received", iter => (decimal)iter.EnergyReceived);
            _AddTeamStats(match, "Energy sent", iter => (decimal)iter.EnergySend);
            _AddTeamStats(match, "Energy used", iter => (decimal)iter.EnergyUsed);

            _AddTeamStats(match, "Metal excess", iter => (decimal)iter.MetalExcess);
            _AddTeamStats(match, "Metal produced", iter => (decimal)iter.MetalProduced);
            _AddTeamStats(match, "Metal excess %",
                iter => (decimal)(iter.MetalExcess / Math.Max(1d, iter.MetalProduced)) * 100m);
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
            
            if (output != null) {
                _HasExtraStats = true;
                _AddOutputTeamStats(match, "Army value", iter => (decimal)iter.ArmyValue);
                _AddOutputTeamStats(match, "Total value", iter => (decimal)iter.TotalValue);
                _AddOutputTeamStats(match, "Eco value", iter => (decimal)iter.EcoValue);
                _AddOutputTeamStats(match, "Util value", iter => (decimal)iter.UtilValue);
                _AddOutputTeamStats(match, "Defense value", iter => (decimal)iter.DefenseValue);
                _AddOutputTeamStats(match, "Other value", iter => (decimal)iter.OtherValue);
                _AddOutputTeamStats(match, "Build power total", iter => (decimal)iter.BuildPowerAvailable);
                _AddOutputTeamStats(match, "Build power used", iter => (decimal)iter.BuildPowerUsed);
                _AddOutputTeamStats(match, "Build power usage",
                    iter => (decimal)(iter.BuildPowerUsed / Math.Max(1d, iter.BuildPowerAvailable)) * 100m);
                _AddOutputTeamStats(match, "Metal current", iter => (decimal)iter.MetalCurrent);
                _AddOutputTeamStats(match, "Energy current", iter => (decimal)iter.EnergyCurrent);
            }

            _TeamStatKeys = new ObservableCollection<string>(_TeamStats.Keys);
            _SelectedTeamStatKey = _TeamStatKeys[0];
            _SelectedTeamStat = _TeamStats.GetValueOrDefault(_SelectedTeamStatKey)!;

            MilestoneVisualElements.Add(new LineVisualElement(10, ColorUtil.ToPaint(Match.AllyTeams[0].HexColor), "test 1"));
            MilestoneVisualElements.Add(new LineVisualElement(9, new SolidColorPaint(SKColors.Red), "test 2"));
            MilestoneVisualElements.Add(new LineVisualElement(8, new SolidColorPaint(SKColors.Red), "test 3"));
            MilestoneVisualElements.Add(new LineVisualElement(7, new SolidColorPaint(SKColors.Red), "test 4"));
            MilestoneVisualElements.Add(new LineVisualElement(6, new SolidColorPaint(SKColors.Red), "test 5"));
            MilestoneVisualElements.Add(new LineVisualElement(6, new SolidColorPaint(SKColors.Red), "test 5"));
            MilestoneVisualElements.Add(new LineVisualElement(6, new SolidColorPaint(SKColors.Red), "test 5"));
            MilestoneVisualElements.Add(new LineVisualElement(6, new SolidColorPaint(SKColors.Red), "test 5"));
            MilestoneVisualElements.Add(new LineVisualElement(6, new SolidColorPaint(SKColors.Red), "test 5"));
            MilestoneVisualElements.Add(new LineVisualElement(6, new SolidColorPaint(SKColors.Red), "test 5"));
        }

        public BarMatchViewModel Match { get; private set; } = new();

        public GameOutput? Output { get; private set; } = null;

        [ObservableProperty]
        private bool _HasExtraStats = false;

        [ObservableProperty]
        private int _AccordianIndex = 0;

        private const int SECTION_BASICS = 0;
        private const int SECTION_UNIT_VALUE = 1;
        private const int SECTION_BUILD_POWER = 2;
        private const int SECTION_METAL_ECO = 3;
        private const int SECTION_ENERGY_ECO = 4;
        private const int SECTION_UNIT = 5;

        public double[] Values1 { get; set; } = [2, 1, 3, 5, 3, 4, 6];

        public int[] Values2 { get; set; } = [4, 2, 5, 2, 4, 5, 3];

        public bool IsSectionBasicsOpened {
            get => AccordianIndex == SECTION_BASICS;
            set { if (value) { AccordianIndex = SECTION_BASICS; } else if (AccordianIndex == SECTION_BASICS) { AccordianIndex = -1; } }
        }

        public bool IsSectionUnitValueOpened {
            get => AccordianIndex == SECTION_UNIT_VALUE;
            set { if (value) { AccordianIndex = SECTION_UNIT_VALUE; } else if (AccordianIndex == SECTION_UNIT_VALUE) { AccordianIndex = -1; } }
        }

        public bool IsSectionBuildPowerOpened {
            get => AccordianIndex == SECTION_BUILD_POWER;
            set { if (value) { AccordianIndex = SECTION_BUILD_POWER; } else if (AccordianIndex == SECTION_BUILD_POWER) { AccordianIndex = -1; } }
        }

        public bool IsSectionMetalEcoOpened {
            get => AccordianIndex == SECTION_METAL_ECO;
            set { if (value) { AccordianIndex = SECTION_METAL_ECO; } else if (AccordianIndex == SECTION_METAL_ECO) { AccordianIndex = -1; } }
        }

        public bool IsSectionEnergyEcoOpened {
            get => AccordianIndex == SECTION_ENERGY_ECO;
            set { if (value) { AccordianIndex = SECTION_ENERGY_ECO; } else if (AccordianIndex == SECTION_ENERGY_ECO) { AccordianIndex = -1; } }
        }

        public bool IsSectionUnitOpened {
            get => AccordianIndex == SECTION_UNIT;
            set { if (value) { AccordianIndex = SECTION_UNIT; } else if (AccordianIndex == SECTION_UNIT) { AccordianIndex = -1; } }
        }

        partial void OnAccordianIndexChanged(int value) {
            OnPropertyChanged(nameof(IsSectionBasicsOpened));
            OnPropertyChanged(nameof(IsSectionUnitValueOpened));
            OnPropertyChanged(nameof(IsSectionBuildPowerOpened));
            OnPropertyChanged(nameof(IsSectionMetalEcoOpened));
            OnPropertyChanged(nameof(IsSectionEnergyEcoOpened));
            OnPropertyChanged(nameof(IsSectionUnitOpened));
        }

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

        [ObservableProperty]
        private ObservableCollection<IChartElement> _MilestoneVisualElements = [];

        /// <summary>
        ///     select a team stats to show
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="InvalidOperationException"></exception>
        [RelayCommand]
        public void SelectTeamStatsKey(string key) {
            if (TeamStatKeys.Contains(key) == false) {
                _Logger.LogWarning($"cannot show team stats key, TeamStatKeys does not contain [key={key}] [gameID={Match.GameID}]");
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

        private void _AddOutputTeamStats(BarMatch match, string name, Func<GameEventExtraStatUpdate, decimal> selector) {
            if (Output == null) {
                return;
            }

            ChartSeriesCollection coll = new();
            coll.Labels = Output.ExtraStats.Select(iter => iter.Frame).Distinct().Order().Select(iter => {
                return TimeSpan.FromSeconds(iter / 30d).GetRelativeFormat();
            }).ToList();

            foreach (BarMatchAllyTeamViewModel allyTeam in Match.AllyTeams) {
                foreach (BarMatchTeamViewModel team in allyTeam.Teams) {
                    List<GameEventExtraStatUpdate> ts = Output.ExtraStats.Where(iter => iter.TeamID == team.TeamID).OrderBy(iter => iter.Frame).ToList();

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
