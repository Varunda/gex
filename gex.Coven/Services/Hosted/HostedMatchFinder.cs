using gex.Common.Models.Match;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models;
using gex.Coven.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Hosted {
    public class HostedMatchFinder : IHostedService {

        private readonly ILogger<HostedMatchFinder> _Logger;
        private readonly DemofileWatcher _DemofileWatcher;
        private readonly BarMatchRepository _MatchRepository;

        private readonly MainViewModel _MainViewModel;

        public HostedMatchFinder(ILogger<HostedMatchFinder> logger,
            DemofileWatcher demofileWatcher, BarMatchRepository matchRepository,
            MainViewModel mainViewModel) {

            _Logger = logger;
            _DemofileWatcher = demofileWatcher;
            _MatchRepository = matchRepository;
            _MainViewModel = mainViewModel;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _Logger.LogInformation("starting");
            _DemofileWatcher.NewMatchReady += _DemofileWatcher_NewMatchReady;

            _ = _DemofileWatcher.LoadAll();

            new Task(async () => {
                try {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                    List<BarMatch> matches = await _MatchRepository.GetAll(cts.Token);
                    _Logger.LogInformation($"loaded matches [count={matches.Count}]");
                    foreach (BarMatch match in matches) {
                        _MainViewModel.AddMatch(match);
                    }
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to load all previous matches");
                }
            }, cancellationToken).Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _DemofileWatcher.NewMatchReady -= _DemofileWatcher_NewMatchReady;
            return Task.CompletedTask;
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
            _MainViewModel.AddMatch(match);
        }

    }
}
