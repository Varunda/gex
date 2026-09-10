using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class MatchListViewModel : ViewModelBase {

        private readonly ILogger<MainViewModel> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarDemofileParser _Parser;
        private readonly UserOptionsService _UserOptionsService;
        private readonly DemofileWatcher _DemofileWatcher;

        public MatchListViewModel() {
            _Logger = App.Current.Services.GetRequiredService<ILogger<MainViewModel>>();
            _MatchRepository = App.Current.Services.GetRequiredService<BarMatchRepository>();
            _Parser = App.Current.Services.GetRequiredService<BarDemofileParser>();
            _UserOptionsService = App.Current.Services.GetRequiredService<UserOptionsService>();
            _DemofileWatcher = App.Current.Services.GetRequiredService<DemofileWatcher>();

            Init();
        }

        [ObservableProperty]
        private ObservableCollection<BarMatchViewModel> _Matches = [];

        [ObservableProperty]
        private string _SortField = "StartTime";

        private async void Init() {
            _DemofileWatcher.NewMatchReady += _DemofileWatcher_NewMatchReady;

            _ = _DemofileWatcher.LoadAll();

            try {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                List<BarMatch> matches = await _MatchRepository.GetAll(cts.Token);

                foreach (BarMatch match in matches) {
                    AddMatch(match);
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed during init");
            }
        }

        public void AddMatch(BarMatch match) {
            if (Matches.Count == 0) {
                Matches.Add(new BarMatchViewModel(match));
            } else {
                for (int i = 0; i < Matches.Count; ++i) {
                    BarMatchViewModel vm = Matches[i];
                    if (match.StartTime >= vm.StartTime) {
                        Matches.Insert(i, new BarMatchViewModel(match));
                        break;
                    }
                }
            }

            Matches = new ObservableCollection<BarMatchViewModel>(Matches.OrderByDescending((BarMatchViewModel iter) => {
                return iter.StartTime;
            }));
        }

        [RelayCommand]
        private async Task Open(object? parameter) {
            if (parameter is not string gameID) {
                return;
            }
            _Logger.LogInformation($"viewing match [gameID={gameID}]");

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            BarMatch? match = await _MatchRepository.GetByID(gameID, cts.Token);
            if (match == null) {
                _Logger.LogError($"failed to find match in Open command [gameID={gameID}]");
                return;
            }

            UserOptions userOptions = _UserOptionsService.Load();

            byte[] bytes = [];
            string replayFileName = Path.Join(userOptions.ReplayFolder, match.FileName);
            if (File.Exists(match.FileName)) {
                bytes = File.ReadAllBytes(match.FileName);
            } else if (File.Exists(replayFileName)) {
                bytes = File.ReadAllBytes(replayFileName);
            } else {
                _Logger.LogError($"failed to find demofile [FileName={match.FileName}]");
                return;
            }

            Result<BarMatch, string> parsed = await _Parser.Parse(match.FileName, bytes, new DemofileParserOptions() {

            }, cts.Token);

            if (parsed.IsOk == false) {
                _Logger.LogError($"failed to parse match from demofile [gameID={gameID}] [error={parsed.Error}]");
                return;
            }

            MatchWindowViewModel vm = new(parsed.Value);

            MatchWindow win = new() {
                DataContext = vm
            };

            win.Show();
        }

        private async void _DemofileWatcher_NewMatchReady(object sender, BarMatch match) {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            BarMatch? existingMatch = await _MatchRepository.GetByID(match.ID, cts.Token);
            if (existingMatch != null) {
                _Logger.LogWarning($"match already exists [gameID={match.ID}]");
                return;
            }

            _Logger.LogInformation($"adding new match to DB [gameID={match.ID}]");
            await _MatchRepository.Insert(match, cts.Token);
            AddMatch(match);
        }

    }
}
