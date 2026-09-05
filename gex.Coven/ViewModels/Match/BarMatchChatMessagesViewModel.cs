using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchChatMessagesViewModel : ViewModelBase {

        public BarMatchChatMessagesViewModel() {

        }

        public BarMatchChatMessagesViewModel(BarMatch match) {
            _Messages = new ObservableCollection<BarMatchChatMessageViewModel>(
                match.ChatMessages.Select(iter => new BarMatchChatMessageViewModel(match, iter))
            );
        }

        [ObservableProperty]
        private ObservableCollection<BarMatchChatMessageViewModel> _Messages = [];

    }

    public partial class BarMatchChatMessageViewModel : ViewModelBase {

        public BarMatchChatMessageViewModel() {

        }

        public BarMatchChatMessageViewModel(BarMatch match, BarMatchChatMessage message) {
            _Message = message.Message;
            _Timestamp = TimeSpan.FromSeconds(message.GameTimestamp).GetRelativeFormat();

            _Source = _GetIdName(match, message.FromId);
            _SourceColor = _GetIdColor(match, message.FromId);

            _Target = _GetIdName(match, message.ToId);
            _TargetColor = _GetIdColor(match, message.ToId);
        }

        [ObservableProperty]
        private string _Timestamp = "";

        [ObservableProperty]
        private string _Source = "";

        [ObservableProperty]
        private IBrush _SourceColor = Brushes.White;

        [ObservableProperty]
        private string _Target = "";

        [ObservableProperty]
        private IBrush _TargetColor = Brushes.White;

        [ObservableProperty]
        private string _Message = "";

        private static string _GetIdName(BarMatch match, int playerID) {
            if (playerID == 255) {
                return "HOST";
            } else if (playerID == 254) {
                return "Everyone";
            } else if (playerID == 253) {
                return "Spec";
            } else if (playerID == 252) {
                return "Team";
            } else {
                return match.Players.FirstOrDefault(iter => iter.PlayerID == playerID)?.Name
                    ?? match.Spectators.FirstOrDefault(iter => iter.PlayerID == playerID)?.Name
                    ?? $"<missing {playerID}>";
            }
        }

        private static readonly IBrush _EveryoneBrush = SolidColorBrush.Parse("#ffffff");
        private static readonly IBrush _HostBrush = SolidColorBrush.Parse("#ff00ff");
        private static readonly IBrush _SpecBrush = SolidColorBrush.Parse("#ffff00");
        private static readonly IBrush _TeamBrush = SolidColorBrush.Parse("#00ff00");

        private static IBrush _GetIdColor(BarMatch match, int playerID, int? allyTeamID = null) {
            if (playerID == 255) {
                return _HostBrush;
            } else if (playerID == 254) {
                return _EveryoneBrush;
            } else if (playerID == 253) {
                return _SpecBrush;
            } else if (playerID == 252) {
                if (allyTeamID != null) {
                    int color = match.Teams.Find(iter => iter.AllyTeamID == allyTeamID.Value)?.Color ?? 0;

                    return new SolidColorBrush(new Color(
                        r: (byte)((color >> 16) & 0xFF),
                        g: (byte)((color >> 8) & 0xFF),
                        b: (byte)((color >> 0) & 0xFF),
                        a: 0xFF
                    ));
                }
                return _TeamBrush;
            } else {
                BarMatchPlayer? player = match.Players.FirstOrDefault(iter => iter.PlayerID == playerID);
                if (player != null) {
                    BarMatchTeam? team = match.Teams.FirstOrDefault(iter => iter.TeamID == player.TeamID);

                    int color = team?.Color ?? 0;

                    return new SolidColorBrush(new Color(
                        r: (byte)((color >> 16) & 0xFF),
                        g: (byte)((color >> 8) & 0xFF),
                        b: (byte)((color >> 0) & 0xFF),
                        a: 0xFF
                    ));
                }

                BarMatchSpectator? spec = match.Spectators.FirstOrDefault(iter => iter.PlayerID == playerID);
                if (spec != null) {
                    return _SpecBrush;
                }

                return SolidColorBrush.Parse("#aaaaaa");
            }
        }

    }

}
