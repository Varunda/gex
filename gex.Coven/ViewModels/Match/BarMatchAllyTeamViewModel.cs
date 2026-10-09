using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchAllyTeamViewModel : ViewModelBase {

        public BarMatchAllyTeamViewModel() {

        }

        public BarMatchAllyTeamViewModel(BarMatch match, BarMatchAllyTeam allyTeam) {
            Name = $"Team {allyTeam.AllyTeamID + 1}";
            Won = allyTeam.Won;

            foreach (BarMatchTeam team in match.Teams.OrderBy(iter => iter.TeamID)) {
                if (team.AllyTeamID != allyTeam.AllyTeamID) {
                    continue;
                }

                _Teams.Add(new BarMatchTeamViewModel(match, team));
                if (ColorBrush == Brushes.Transparent) {
                    HexColor = _Teams[0].HexColor;
                    ColorBrush = _Teams[0].ColorBrush;
                    BackgroundColorBrush = new SolidColorBrush(((SolidColorBrush)ColorBrush).Color, 0.2d);
                }
            }

            TeamCount = _Teams.Count;
        }

        [ObservableProperty]
        public partial string Name { get; set; } = "";

        [ObservableProperty]
        public partial bool Won { get; set; } = false;

        [ObservableProperty]
        public partial string HexColor { get; set; } = "";

        [ObservableProperty]
        public partial IBrush ColorBrush { get; set; } = Brushes.Transparent;

        [ObservableProperty]
        public partial IBrush BackgroundColorBrush { get; set; } = Brushes.Transparent;

        [ObservableProperty]
        public partial int TeamCount { get; set; } = 0;

        [ObservableProperty]
        private ObservableCollection<BarMatchTeamViewModel> _Teams = new ObservableCollection<BarMatchTeamViewModel>();

        public bool HasUserID(long userID) {
            return Teams.FirstOrDefault(iter => iter.UserIDs.Contains(userID)) != null;
        }

    }
}
