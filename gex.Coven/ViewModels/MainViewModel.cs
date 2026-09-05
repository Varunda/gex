using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Coven.Models;
using gex.Coven.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels;

public partial class MainViewModel : ViewModelBase {

    private readonly ILogger<MainViewModel> _Logger;
    private readonly BarMatchRepository _MatchRepository;
    private readonly BarDemofileParser _Parser;

    public MainViewModel() {
        _Logger = App.Current.Services.GetRequiredService<ILogger<MainViewModel>>();
        _MatchRepository = App.Current.Services.GetRequiredService<BarMatchRepository>();
        _Parser = App.Current.Services.GetRequiredService<BarDemofileParser>();
    }

    [ObservableProperty]
    private ObservableCollection<BarMatchViewModel> _Matches = new ObservableCollection<BarMatchViewModel>();

    public void AddMatch(BarMatch match) {
        Matches.Add(new BarMatchViewModel(match));
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

        byte[] bytes = File.ReadAllBytes(match.FileName);

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

}
