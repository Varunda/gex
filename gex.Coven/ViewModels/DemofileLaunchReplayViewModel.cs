using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models.Match;
using gex.Coven.Services.Bar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class DemofileLaunchReplayViewModel : ViewModelBase {

        private readonly ILogger<DemofileLaunchReplayViewModel> _Logger;
        private readonly CovenGameRunner _GameRunner;

        private CancellationTokenSource _CancelSource;

        public DemofileLaunchReplayViewModel() {
            _Logger = App.Current?.Services?.GetService<ILogger<DemofileLaunchReplayViewModel>>() ?? default!;
            _GameRunner = App.Current?.Services?.GetService<CovenGameRunner>() ?? default!;

            _CancelSource = new CancellationTokenSource();

            try {
                _ = Init();
            } catch (Exception ex) {

            }
        }

        [ObservableProperty]
        public partial BarMatch Match { get; set; } = new();

        [ObservableProperty]
        public partial string GameID { get; set; } = "";

        [ObservableProperty]
        public partial bool EngineDownloaded { get; set; } = false;

        [ObservableProperty]
        public partial bool GameDownloaded { get; set; } = false;

        [ObservableProperty]
        public partial bool MapDownloaded { get; set; } = false;

        public async Task Init() {
            GameID = Match.ID;

            _GameRunner.EngineDownloaded += _GameRunner_EngineDownloaded;
            _GameRunner.GameVersionDownloaded += _GameRunner_GameVersionDownloaded;
            _GameRunner.MapDownloaded += _GameRunner_MapDownloaded;
            _GameRunner.FatalError += _GameRunner_FatalError;

            _Logger.LogInformation($"starting replay [gameID={GameID}]");
            await _GameRunner.LaunchReplay(Match, true, _CancelSource.Token);
            _Logger.LogDebug($"started replay [gameID={GameID}]");
        }

        [RelayCommand]
        public void CancelLaunch() {
            _CancelSource.Cancel();
        }

        private void _GameRunner_FatalError(object sender, string gameID, string error) {
            if (gameID != GameID) {
                return;
            }

            _Logger.LogError($"fatal error while trying to run a game [gameID={gameID}] [error={error}]");
        }

        private void _GameRunner_MapDownloaded(object sender, string gameID) {
            if (gameID != GameID) {
                return;
            }

            MapDownloaded = true;
        }

        private void _GameRunner_GameVersionDownloaded(object sender, string gameID) {
            if (gameID != GameID) {
                return;
            }

            GameDownloaded = true;
        }

        private void _GameRunner_EngineDownloaded(object sender, string gameID) {
            if (gameID != GameID) {
                return;
            }

            EngineDownloaded = true;
        }

    }
}
