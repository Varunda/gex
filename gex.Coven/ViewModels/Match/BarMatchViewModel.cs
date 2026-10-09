using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.Constants;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.Services.Util;
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
            GameID = match.ID;
            Map = match.Map;
            Gamemode = BarGamemode.GetName(match.Gamemode);
            GamemodeID = match.Gamemode;
            StartTime = match.StartTime;
            FileName = match.FileName;
            Engine = match.Engine;
            GameVersion = match.GameVersion;
            DurationMs = (int)TimeSpan.FromSeconds(match.DurationFrameCount / 30f).TotalMilliseconds;
            Duration = TimeSpan.FromSeconds(match.DurationFrameCount / 30f).GetRelativeFormat();

            foreach (BarMatchAllyTeam at in match.AllyTeams.OrderBy(iter => iter.AllyTeamID)) {
                AllyTeams.Add(new BarMatchAllyTeamViewModel(match, at));
            }

            ChatMessages = new ObservableCollection<BarMatchChatMessage>(match.ChatMessages.Select(iter => {
                iter.GameTimestamp = Math.Max(0, iter.GameTimestamp - match.StartOffset);
                return iter;
            }));

            StorageUtil? storageUtil = App.Current?.Services?.GetService<StorageUtil>();
            HasActionLog = storageUtil?.HasActionLog(match.ID) ?? false;

            UserOptionsService? userOptionsService = App.Current?.Services?.GetService<UserOptionsService>();
            UserOptions? userOptions = userOptionsService?.Load();
            if (userOptions != null && userOptions.TargetUserId != null) {
                foreach (BarMatchAllyTeamViewModel atvm in AllyTeams) {
                    if (atvm.HasUserID(userOptions.TargetUserId.Value)) {
                        Won = atvm.Won;
                        break;
                    }
                }
            }

            if (GamemodeID == BarGamemode.FFA) {
                TeamSizes = $"{AllyTeams.Count}-way FFA";
            } else {
                TeamSizes = $"{string.Join(" v ", AllyTeams.Select(iter => iter.TeamCount))}";
            }

            if (match.GameSettings.GetString("ranked_game", "0") == "1") {
                Tags.Add(new BarMatchTag("Ranked", Brushes.Teal, null));
            } else {
                Tags.Add(new BarMatchTag("Unranked", Brushes.Orange, null));
            }

            if (match.GameSettings.GetString("zombies", "disabled") != "disabled") {
                Tags.Add(new BarMatchTag("Zombies", Brushes.ForestGreen, null));
            }

            if (match.GameSettings.GetString("map_waterislava", "") == "1") {
                Tags.Add(new BarMatchTag("Lava", Brushes.OrangeRed, null));
            }

        }

        public BarMatch Match { get; } = new();

        [ObservableProperty]
        public partial string GameID { get; set; } = "";

        [ObservableProperty]
        public partial string Map { get; set; } = "";

        [ObservableProperty]
        public partial string Gamemode { get; set; } = "";

        [ObservableProperty]
        public partial string TeamSizes { get; set; } = "";

        [ObservableProperty]
        public partial int GamemodeID { get; set; } = 0;

        [ObservableProperty]
        public partial DateTime StartTime { get; set; } = DateTime.Now;

        [ObservableProperty]
        public partial long DurationMs { get; set; } = 0;

        [ObservableProperty]
        public partial string Duration { get; set; } = "";

        [ObservableProperty]
        public partial string FileName { get; set; } = "";

        [ObservableProperty]
        public partial string Engine { get; set; } = "";

        [ObservableProperty]
        public partial string GameVersion { get; set; } = "";

        [ObservableProperty]
        public partial bool HasActionLog { get; set; } = false;

        [ObservableProperty]
        public partial bool? Won { get; set; } = null;

        [ObservableProperty]
        public partial ObservableCollection<BarMatchAllyTeamViewModel> AllyTeams { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<BarMatchChatMessage> ChatMessages { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<BarMatchTag> Tags { get; set; } = [];

    }
}
