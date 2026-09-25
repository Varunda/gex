using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Event;
using gex.Common.Models.Match;
using gex.Coven.Code;
using gex.Coven.Code.Chart;
using gex.Coven.Models.Chart;
using gex.Coven.Models.Match;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Layouts;
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

        public BarMatchViewTeamStats(MatchWindowViewModel vm) {
            _Logger = App.Current?.Services?.GetService<ILogger<MatchWindowViewModel>>() ?? default!;

            Match = vm.Match;
            Output = vm.Output;

            BarMatch match = vm.Match.Match;

            _AddTeamStats(match, vm.Entities, "Damage dealt", iter => (decimal)iter.DamageDealt);
            _AddTeamStats(match, vm.Entities, "Damage taken", iter => (decimal)iter.DamageReceived);

            _AddTeamStats(match, vm.Entities, "Energy excess", iter => (decimal)iter.EnergyExcess);
            _AddTeamStats(match, vm.Entities, "Energy excess %",
                iter => (decimal)(iter.EnergyExcess / Math.Max(1d, iter.EnergyProduced)) * 100m);
            _AddTeamStats(match, vm.Entities, "Energy produced", iter => (decimal)iter.EnergyProduced);
            _AddTeamStats(match, vm.Entities, "Energy received", iter => (decimal)iter.EnergyReceived);
            _AddTeamStats(match, vm.Entities, "Energy sent", iter => (decimal)iter.EnergySend);
            _AddTeamStats(match, vm.Entities, "Energy used", iter => (decimal)iter.EnergyUsed);

            _AddTeamStats(match, vm.Entities, "Metal excess", iter => (decimal)iter.MetalExcess);
            _AddTeamStats(match, vm.Entities, "Metal produced", iter => (decimal)iter.MetalProduced);
            _AddTeamStats(match, vm.Entities, "Metal excess %",
                iter => (decimal)(iter.MetalExcess / Math.Max(1d, iter.MetalProduced)) * 100m);
            _AddTeamStats(match, vm.Entities, "Metal received", iter => (decimal)iter.MetalReceived);
            _AddTeamStats(match, vm.Entities, "Metal sent", iter => (decimal)iter.MetalSend);
            _AddTeamStats(match, vm.Entities, "Metal used", iter => (decimal)iter.MetalUsed);

            _AddTeamStats(match, vm.Entities, "Units captured", iter => (decimal)iter.UnitsCaptured);
            _AddTeamStats(match, vm.Entities, "Units died", iter => (decimal)iter.UnitsDied);
            _AddTeamStats(match, vm.Entities, "Units killed", iter => (decimal)iter.UnitsKilled);
            _AddTeamStats(match, vm.Entities, "Units lost to capture", iter => (decimal)iter.UnitsOutCaptured);
            _AddTeamStats(match, vm.Entities, "Units made", iter => (decimal)iter.UnitsProduced);
            _AddTeamStats(match, vm.Entities, "Units received", iter => (decimal)iter.UnitsReceived);
            _AddTeamStats(match, vm.Entities, "Units sent", iter => (decimal)iter.UnitsSent);
            
            if (Output != null) {
                _HasExtraStats = true;
                _AddOutputTeamStats(match, vm.Entities, "Army value", iter => (decimal)iter.ArmyValue);
                _AddOutputTeamStats(match, vm.Entities, "Total value", iter => (decimal)iter.TotalValue);
                _AddOutputTeamStats(match, vm.Entities, "Eco value", iter => (decimal)iter.EcoValue);
                _AddOutputTeamStats(match, vm.Entities, "Util value", iter => (decimal)iter.UtilValue);
                _AddOutputTeamStats(match, vm.Entities, "Defense value", iter => (decimal)iter.DefenseValue);
                _AddOutputTeamStats(match, vm.Entities, "Other value", iter => (decimal)iter.OtherValue);
                _AddOutputTeamStats(match, vm.Entities, "Build power total", iter => (decimal)iter.BuildPowerAvailable);
                _AddOutputTeamStats(match, vm.Entities, "Build power used", iter => (decimal)iter.BuildPowerUsed);
                _AddOutputTeamStats(match, vm.Entities, "Build power usage",
                    iter => (decimal)(iter.BuildPowerUsed / Math.Max(1d, iter.BuildPowerAvailable)) * 100m);
                _AddOutputTeamStats(match, vm.Entities, "Metal current", iter => (decimal)iter.MetalCurrent);
                _AddOutputTeamStats(match, vm.Entities, "Energy current", iter => (decimal)iter.EnergyCurrent);
            }

            _TeamStatKeys = new ObservableCollection<string>(_TeamStats.Keys);
            SelectTeamStatsKey(_TeamStatKeys[0]);

            BarMatchTeamStats? firstFrame = match.TeamStats.FirstOrDefault(iter => iter.Frame == 0);
            BarMatchTeamStats? nextFrame = match.TeamStats.OrderBy(iter => iter.Frame)
                .FirstOrDefault(iter => iter.Frame > (firstFrame?.Frame ?? 0));

            long frameDelta = (nextFrame?.Frame ?? 4500) - (firstFrame?.Frame ?? 0);

            foreach (BarMatchMilestone milestone in vm.Milestones.Milestones.OrderBy(iter => iter.Frame)) {
                MilestoneVisualElements.Add(new LineVisualElement(
                    (double)milestone.Frame / (double)frameDelta,
                    ColorUtil.ToPaint(milestone.Entity.HexColor),
                    milestone.Action
                ));
            }

            VisibleMilestones = new ObservableCollection<IChartElement>(MilestoneVisualElements);
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

        [RelayCommand]
        public void ChartClicked(PointerCommandArgs args) {
            IChartLegend? legend = args.Chart.Legend;
            if (legend == null) {
                return;
            }

            if (legend is not CovenLegend leg) {
                return;
            }

            double mx = args.PointerPosition.X;
            double my = args.PointerPosition.Y;

            if (mx < leg.Geometry.X || mx > leg.Geometry.X + leg.Geometry.Width) {
                return;
            }

            if (my < leg.Geometry.Y || my > leg.Geometry.Y + leg.Geometry.Height) {
                return;
            }

            if (leg.Content is not StackLayout content) {
                return;
            }

            foreach (IDrawnElement<SkiaSharpDrawingContext> child in content.Children) {
                if ((mx < child.X) || (my < child.Y)) {
                    continue;
                }

                LvcSize size = child.Measure();
                if ((mx > child.X + size.Width) || (my > child.Y + size.Height)) {
                    continue;
                }

                if (child is LegendItem item) {
                    ChartSeries? series = SelectedTeamStat.Series.FirstOrDefault(iter => iter.Name == item.Name);
                    if (series == null) {
                        _Logger.LogDebug($"failed to find series to toggle visibility of [name={item.Name}]");
                    } else {
                        series.Visible = !series.Visible;
                    }

                    VisibleMilestones = new ObservableCollection<IChartElement>(MilestoneVisualElements.Where(iter => {
                        return true;
                    }));

                    break;
                }
            }
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
        private ObservableCollection<ChartSeries> _SelectedTeamStatSeries = new();

        [ObservableProperty]
        private ObservableCollection<string> _SelectedTeamStatLabels = new();

        [ObservableProperty]
        private ObservableCollection<IChartElement> _MilestoneVisualElements = [];

        [ObservableProperty]
        private ObservableCollection<IChartElement> _VisibleMilestones = [];

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
            SelectedTeamStatSeries = new ObservableCollection<ChartSeries>(series.Series);
            SelectedTeamStatLabels = new ObservableCollection<string>(series.Labels);
        }

        /// <summary>
        ///     create a new chart series 
        /// </summary>
        /// <param name="match"></param>
        /// <param name="name"></param>
        /// <param name="selector"></param>
        private void _AddTeamStats(BarMatch match, List<BarMatchEntity> entities, string name, Func<BarMatchTeamStats, decimal> selector) {
            ChartSeriesCollection coll = new();
            coll.Labels = match.TeamStats.Select(iter => iter.Frame).Distinct().Order().Select(iter => {
                return TimeSpan.FromSeconds(iter / 30d).GetRelativeFormat();
            }).ToList();

            foreach (BarMatchEntity entity in entities) {
                List<BarMatchTeamStats> ts = match.TeamStats.Where(iter => entity.TeamIDs.Contains(iter.TeamID)).OrderBy(iter => iter.Frame).ToList();

                List<int> frames = ts.Select(iter => iter.Frame).Distinct().Order().ToList();

                ChartSeries cs = new() {
                    Name = entity.Name,
                    Values = [ ..frames.Select(frame => {
                        return ts.Where(iter => iter.Frame == frame).Sum(selector);
                    })],
                    Color = ColorUtil.ToPaint(entity.HexColor)
                };

                cs.Color.StrokeThickness = 2;
                coll.Series.Add(cs);
            }

            TeamStats.Add(name, coll);
        }

        private void _AddOutputTeamStats(BarMatch match, List<BarMatchEntity> entities, string name, Func<GameEventExtraStatUpdate, decimal> selector) {
            if (Output == null) {
                return;
            }

            ChartSeriesCollection coll = new();
            coll.Labels = Output.ExtraStats.Select(iter => iter.Frame).Distinct().Order().Select(iter => {
                return TimeSpan.FromSeconds(iter / 30d).GetRelativeFormat();
            }).ToList();

            foreach (BarMatchEntity entity in entities) {
                List<GameEventExtraStatUpdate> ts = Output.ExtraStats.Where(iter => entity.TeamIDs.Contains(iter.TeamID)).OrderBy(iter => iter.Frame).ToList();

                List<long> frames = ts.Select(iter => iter.Frame).Distinct().Order().ToList();

                ChartSeries cs = new() {
                    Name = entity.Name,
                    Values = [ ..frames.Select(frame => {
                        return ts.Where(iter => iter.Frame == frame).Sum(selector);
                    })],
                    Color = ColorUtil.ToPaint(entity.HexColor)
                };

                cs.Color.StrokeThickness = 2;
                coll.Series.Add(cs);
            }

            TeamStats.Add(name, coll);
        }

    }
}
