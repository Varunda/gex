using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.Constants;
using gex.Common.Models.Match;
using HarfBuzzSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchTeamViewModel : ViewModelBase {

        public BarMatchTeamViewModel() {

        }

        public BarMatchTeamViewModel(BarMatch match, BarMatchTeam team) {
            _TeamID = team.TeamID;
            _Color = TeamColorLut.Lut.GetValueOrDefault(team.Color);
            _HexColor = $"#{Color.ToString("X2").PadLeft(6, '0')}";
            _StartPositionLabel = team.StartSpotLabel;
            _Faction = team.Faction;
            _Handicap = team.Handicap;

            IEnumerable<BarMatchPlayer> players = match.Players.Where(iter => iter.TeamID == team.TeamID);
            if (players.Any()) {
                _Name = string.Join(" & ", players.Select(iter => iter.Name));
            } else {
                BarMatchAiPlayer? ai = match.AiPlayers.FirstOrDefault(iter => iter.TeamID == team.TeamID);
                _Name = ai?.Name ?? "<no player>";
            }

        }

        [ObservableProperty]
        private int _TeamID = 0;

        [ObservableProperty]
        private string _Name = "";

        [ObservableProperty]
        private string _HexColor = "";

        [ObservableProperty]
        private int _Color = 0;

        [ObservableProperty]
        private string? _StartPositionLabel = null;

        [ObservableProperty]
        private string _Faction = "";

        [ObservableProperty]
        private float _Handicap = 0f;

    }
}
