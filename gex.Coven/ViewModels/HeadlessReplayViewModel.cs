using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models.Match;
using gex.Coven.Services.Bar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class HeadlessReplayViewModel : ViewModelBase {

        private readonly ILogger<HeadlessReplayViewModel> _Logger;
        private readonly CovenGameRunner _GameRunner;

        private readonly CancellationTokenSource _CancelTokenSource;

        public event EventHandler? OnRequestClose;

        public HeadlessReplayViewModel() {
            _Logger = App.Current?.Services?.GetService<ILogger<HeadlessReplayViewModel>>() ?? default!;
            _GameRunner = App.Current?.Services?.GetService<CovenGameRunner>() ?? default!;

            _CancelTokenSource = new();
            _ = Task.Run(() => {
                _ = StartReplay();
            });

            _Title = "gex.Coven - Headless replay";
        }

        public async Task StartReplay() {
            Title = "gex.Coven - Headless replay: Loading game";
            Status = "Loading game";
            GameID = Match.ID;

            _GameRunner.HeadlessProgressUpdate += _GameRunner_HeadlessProgressUpdate;
            _GameRunner.HeadlessDone += _GameRunner_HeadlessDone;
            _GameRunner.HeadlessStdoutLine += _GameRunner_HeadlessStdoutLine;
            _GameRunner.HeadlessStderrLine += _GameRunner_HeadlessStderrLine;

            _Logger.LogInformation($"starting replay [gameID={GameID}]");
            await _GameRunner.LaunchReplay(Match, true, _CancelTokenSource.Token);
            _Logger.LogDebug($"started replay [gameID={GameID}]");

            ProgressBrush = Brushes.Teal;
            ProgressLoading = true;
            Progress = 100;
        }

        private void _GameRunner_HeadlessDone(object sender, string gameID) {
            if (gameID != GameID) {
                return;
            }

            _Logger.LogInformation($"headless replay done [gameID={gameID}]");
            OnRequestClose?.Invoke(this, new EventArgs());
        }

        private void _GameRunner_HeadlessProgressUpdate(object sender, Common.Models.HeadlessRunStatus status) {
            // multiple replays can be run at the same time, ensure this only handles the one in this window
            if (status.GameID != GameID) {
                return;
            }

            ReplayStarted = status.Simulating;
            LastFrame = status.Frame;
            FramesDuration = status.DurationFrames;
            Fps = status.Fps;

            Progress = (double)LastFrame / FramesDuration * 100d;
            Eta = ((double)FramesDuration - LastFrame) / Math.Max(0.01d, Fps);

            if (status.Simulating == true) {
                Status = "Simulating";
                ProgressLoading = false;
                ProgressBrush = Brushes.Blue;
            } else {
                Progress = 100;
                ProgressLoading = true;
                Status = $"Loading game";
                if (status.LoadingStep != "") {
                    Status += $": {status.LoadingStep}";
                }
            }

            Title = $"gex.Coven - Headless replay: {Status}";
        }

        private void _GameRunner_HeadlessStdoutLine(object sender, string gameID, string line) {
            if (gameID != GameID) {
                return;
            }

            Stdout.Insert(0, line);
            if (Stdout.Count > 100) {
                Stdout.RemoveAt(Stdout.Count - 1);
            }
        }

        private void _GameRunner_HeadlessStderrLine(object sender, string gameID, string line) {
            if (gameID != GameID) {
                return;
            }

            Stdout.Add(line);
            if (Stdout.Count > 100) {
                Stdout.RemoveAt(0);
            }
        }

        [RelayCommand]
        public void CancelReplay() {
            _CancelTokenSource.Cancel();
        }

        [ObservableProperty]
        private BarMatch _Match = new();

        [ObservableProperty]
        private string _GameID = "";

        [ObservableProperty]
        private string _Title = "";

        [ObservableProperty]
        private bool _ReplayStarted = false;

        [ObservableProperty]
        private string _Status = "";

        [ObservableProperty]
        private long _LastFrame = 0;

        [ObservableProperty]
        private long _FramesDuration = 0;

        [ObservableProperty]
        private double _Progress = 0d;

        [ObservableProperty]
        private double _Fps = 0d;

        [ObservableProperty]
        private double _Eta = 0d;

        [ObservableProperty]
        private bool _ProgressLoading = true;

        [ObservableProperty]
        private IBrush _ProgressBrush = Brushes.Teal;

        [ObservableProperty]
        private ObservableCollection<string> _Stdout = [];

    }
}
