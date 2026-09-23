using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Bar;
using gex.Common.Services.Db;
using gex.Common.Services.Metrics;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Code;
using gex.Coven.Models;
using gex.Coven.Services;
using gex.Coven.Services.Db;
using gex.Coven.Services.Hosted;
using gex.Coven.ViewModels;
using gex.Coven.Views;
using gex.Coven.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using R86.Avalonia.Hosting;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven;

public partial class App : HostedApplication<App> {

    public static Window MainWindow = null!;

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);

        Stream fontStream = AssetLoader.Open(new Uri("avares://gex.Coven/Assets/Fonts/AtkinsonHyperlegible-Regular.ttf"));

        LiveCharts.Configure(config => {
            config.UseDefaults();
            config.AddSkiaSharp();
            config.AddDefaultTheme();
            config.HasTextSettings(new TextSettings() {
                DefaultTypeface = SKTypeface.FromStream(fontStream)
            });
        });
    }

    public override void OnFrameworkInitializationCompleted() {
        base.OnFrameworkInitializationCompleted();
    }

    public override async Task StartAsync(CancellationToken cancellationToken) {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow() {
                DataContext = new MainViewModel()
            };

            MainWindow = desktop.MainWindow;
        }

        ILogger<App> logger = Services.GetRequiredService<ILogger<App>>();

        Dispatcher.UIThread.UnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs e) => {
            logger.LogError(e.Exception, $"unhandled UI exception");
            Trace.Write($"unhandled UI exception: {e.Exception}");
        };

        await base.StartAsync(cancellationToken);

        if (Program.PendingDemofileRead != null) {
            ToastService toaster = Services.GetRequiredService<ToastService>();

            string demofile = Program.PendingDemofileRead;
            logger.LogInformation($"launched via sdfz, launching that file [demofile={Program.PendingDemofileRead}]");
            toaster.Show("Loading demofile...", $"Loading demofile {Path.GetFileName(demofile)}", ToastType.INFO, TimeSpan.FromSeconds(5));

            if (File.Exists(demofile) == false) {
                toaster.Show("Failed to open demofile", $"Demofile does not exist", ToastType.ERROR, TimeSpan.FromSeconds(15));
                logger.LogWarning($"demofile does not exist [path={demofile}]");
                return;
            }

            FileInfo fi = new(demofile);
            if (fi.Length >= (1024 * 1024 * 256)) {
                toaster.Show("Failed to open demofile", $"Demofile is more than 256MB", ToastType.ERROR, TimeSpan.FromSeconds(15));
                logger.LogWarning($"refusing to open demofile that is more than 256MB [size={fi.Length}] [path={demofile}]");
                return;
            }

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            BarDemofileParser parser = Services.GetRequiredService<BarDemofileParser>();
            byte[] data = await File.ReadAllBytesAsync(demofile, cts.Token);
            Result<BarMatch, string> parsed = await parser.Parse(Path.GetFileName(demofile), data, new DemofileParserOptions() {
                ParseHeaderOnly = true
            }, cts.Token);

            if (parsed.IsOk == false) {
                toaster.Show("Failed to open demofile", $"Demofile failed to parse", ToastType.ERROR, TimeSpan.FromSeconds(15));
                logger.LogWarning($"failed to parse demofile [error={parsed.Error}] [path={demofile}]");
                return;
            }

            BarMatchRepository matchRepository = Services.GetRequiredService<BarMatchRepository>();
            BarMatch? existingMatch = await matchRepository.GetByID(parsed.Value.ID, CancellationToken.None);
            if (existingMatch != null) {
                using CancellationTokenSource ctsOpen = new(TimeSpan.FromSeconds(15));
                toaster.Show("Opening demofile...", "", ToastType.INFO, TimeSpan.FromSeconds(4));
                await MatchWindow.LoadMatchAndShow(parsed.Value.ID, ctsOpen.Token);
                return;
            }

            logger.LogInformation($"demofile does not exist in repository, copying to demos folder");
        }

    }

}