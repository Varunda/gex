using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Code;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels {

    public partial class MainViewModel : ViewModelBase {

        private readonly ILogger<MainViewModel> _Logger;
        private readonly UserOptionsService _UserOptionsService;

        public ToastService Toasts { get; }
        public DisplayLoggerService DisplayLogger { get; }
        public CovenVersionUtil VersionUtil { get; }

        public MainViewModel() {
            _Logger = App.Current?.Services?.GetService<ILogger<MainViewModel>>() ?? default!;
            _UserOptionsService = App.Current?.Services?.GetService<UserOptionsService>() ?? default!;
            Toasts = App.Current?.Services?.GetService<ToastService>() ?? default!;
            DisplayLogger = App.Current?.Services?.GetService<DisplayLoggerService>() ?? default!;
            VersionUtil = App.Current?.Services?.GetService<CovenVersionUtil>() ?? default!;

            try {
                _Version = VersionUtil.GetCurrentVersion() ?? "<missing>";
                _Logger.LogInformation($"current version loaded [version={_Version}]");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to load file version from assembly");
                _Version = "<errored>";
            }

            UserOptions userOptions = _UserOptionsService.Load();
            if (userOptions.CheckForUpdates == true) {
                Task.Run(async () => {
                    try {
                        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                        Result<CovenVersion, string> ret = await VersionUtil.GetLatest(userOptions.Repository, cts.Token);
                        if (ret.IsOk == false) {
                            _Logger.LogError($"failed to get latest version [error={ret.Error}]");
                            return;
                        }

                        LatestVersion = ret.Value.Tag;
                        CanVersionUpdate = _LatestVersion != _Version;
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"exception while getting latest version");
                    }
                });
            }
        }

        [ObservableProperty]
        private string _Version = "";

        [ObservableProperty]
        private string _LatestVersion = "";

        [ObservableProperty]
        private bool _CanVersionUpdate = false;

        [RelayCommand]
        public void OpenLogs() {
            DisplayLoggerWindow win = new() {
                DataContext = DisplayLogger.Get()
            };

            win.Show();
        }

        [RelayCommand]
        public void OpenUpdater() {
            _Logger.LogInformation($"yeah we're updating now");
        }

    }
}
