using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.Constants;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Coven.ViewModels.Match;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace gex.Coven.ViewModels {

    public partial class BarMatchViewModel : ViewModelBase {

        public BarMatchViewModel() {

        }

        public BarMatchViewModel(BarMatch match) {
            Match = match;

            _GameID = match.ID;
            _Map = match.Map;
            _Gamemode = BarGamemode.GetName(match.Gamemode);
            _GamemodeID = match.Gamemode;
            _StartTime = match.StartTime;
            _FileName = match.FileName;
            _Engine = match.Engine;
            _GameVersion = match.GameVersion;

            _DurationMs = (int)TimeSpan.FromSeconds(match.DurationFrameCount / 30f).TotalMilliseconds;
            _Duration = TimeSpan.FromSeconds(match.DurationFrameCount / 30f).GetRelativeFormat();

            foreach (BarMatchAllyTeam at in match.AllyTeams.OrderBy(iter => iter.AllyTeamID)) {
                _AllyTeams.Add(new BarMatchAllyTeamViewModel(match, at));
            }

            _ChatMessages = new ObservableCollection<BarMatchChatMessage>(match.ChatMessages.Select(iter => {
                iter.GameTimestamp = Math.Max(0, iter.GameTimestamp - match.StartOffset);
                return iter;
            }));
        }

        public BarMatch Match { get; } = new();

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
        private string _Engine = "";

        [ObservableProperty]
        private string _GameVersion = "";

        [ObservableProperty]
        private ObservableCollection<BarMatchAllyTeamViewModel> _AllyTeams = [];

        [ObservableProperty]
        private ObservableCollection<BarMatchChatMessage> _ChatMessages = [];


    }
}
