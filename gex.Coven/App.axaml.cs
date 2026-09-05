using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using gex.Common.Services.Bar;
using gex.Common.Services.Db;
using gex.Common.Services.Metrics;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Models;
using gex.Coven.Services.Db;
using gex.Coven.Services.Hosted;
using gex.Coven.ViewModels;
using gex.Coven.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using R86.Avalonia.Hosting;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven;

public partial class App : HostedApplication<App> {

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);

        LiveCharts.Configure(config => {
            config.AddSkiaSharp();
            config.AddDefaultTheme();
        });
    }

    public override void OnFrameworkInitializationCompleted() {
        base.OnFrameworkInitializationCompleted();
    }

    public override async Task StartAsync(CancellationToken cancellationToken) {
        MainViewModel vm = Current.Services.GetRequiredService<MainViewModel>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow() {
                DataContext = vm
            };
        }

        await base.StartAsync(cancellationToken);
    }

}