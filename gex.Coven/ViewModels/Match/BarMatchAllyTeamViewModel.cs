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
            _Name = $"Team {allyTeam.AllyTeamID + 1}";
            _Won = allyTeam.Won;

            foreach (BarMatchTeam team in match.Teams) {
                if (team.AllyTeamID != allyTeam.AllyTeamID) {
                    continue;
                }
                _Teams.Add(new BarMatchTeamViewModel(match, team));
            }

            _TeamCount = _Teams.Count;
        }

        [ObservableProperty]
        private string _Name = "";

        [ObservableProperty]
        private bool _Won = false;

        [ObservableProperty]
        private int _TeamCount = 0;

        [ObservableProperty]
        private ObservableCollection<BarMatchTeamViewModel> _Teams = new ObservableCollection<BarMatchTeamViewModel>();

    }
}
