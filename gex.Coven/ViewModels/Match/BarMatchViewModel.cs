using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.Constants;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Coven.ViewModels.Match;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class BarMatchViewModel : ViewModelBase {

        public BarMatchViewModel() {

        }

        public BarMatchViewModel(BarMatch match) {
            _GameID = match.ID;
            _Map = match.Map;
            _Gamemode = BarGamemode.GetName(match.Gamemode);
            _GamemodeID = match.Gamemode;
            _StartTime = match.StartTime;
            _DurationMs = match.DurationMs;
            _Duration = TimeSpan.FromMilliseconds(match.DurationMs).GetRelativeFormat();
            _FileName = match.FileName;

            foreach (BarMatchAllyTeam at in match.AllyTeams) {
                _AllyTeams.Add(new BarMatchAllyTeamViewModel(match, at));
            }

            _ChatMessages = new ObservableCollection<BarMatchChatMessage>(match.ChatMessages);
        }

        [ObservableProperty]
        private string _GameID = "";

        [ObservableProperty]
        private string _Map = "";

        [ObservableProperty]
        private string _Gamemode = "";

        [ObservableProperty]
        private int _GamemodeID = 0;

        [ObservableProperty]
        private DateTime _StartTime = DateTime.Now;

        [ObservableProperty]
        private long _DurationMs = 0;

        [ObservableProperty]
        private string _Duration = "";

        [ObservableProperty]
        private string _FileName = "";

        [ObservableProperty]
        private ObservableCollection<BarMatchAllyTeamViewModel> _AllyTeams = [];

        [ObservableProperty]
        private ObservableCollection<BarMatchChatMessage> _ChatMessages = [];


    }
}
