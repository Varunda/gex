using Avalonia;
using gex.Common.Services.Db;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Code;
using gex.Coven.Models;
using gex.Coven.Services.Db;
using gex.Coven.Services.Hosted;
using gex.Coven.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using R86.Avalonia.Hosting;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace gex.Coven;

sealed class Program {

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    public static void Main(string[] args) {
        HostedApplication<App>.AvaloniaApplicationBuilder hostBuilder = App.CreateBuilder(args, BuildAvaloniaApp, () => Host.CreateEmptyApplicationBuilder(null));

        hostBuilder.Configuration
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json")
            .AddInMemoryCollection();

        hostBuilder.Services.AddMemoryCache();

        hostBuilder.Services.AddSingleton<BarDemofileParser>();
        hostBuilder.Services.AddSingleton<MainViewModel>();
        hostBuilder.Services.AddSingleton<IDbHelper, SqLiteDbHelper>();
        hostBuilder.Services.AddSingleton<IDbCreator, SqLiteDbCreator>();
        hostBuilder.Services.AddSingleton<DemofileWatcher>();
        hostBuilder.Services.AddSingleton<LuaCommandParser>();
        hostBuilder.Services.AddSingleton<PolygonStartboxUtil>();
        hostBuilder.Services.AddSingleton<BarMatchRepository>();

        hostBuilder.Services.AddCovenDbServices();

        hostBuilder.Logging.AddFile("logs/gex.Coven-{0:yyyy}-{0:MM}-{0:dd}.log", options => {
            options.FormatLogFileName = fName => {
                return string.Format(fName, DateTime.UtcNow);
            };
            options.FileSizeLimitBytes = (1024 * 1024 * 64); // 64MB
            options.MaxRollingFiles = 10;
            options.MinLevel = LogLevel.Trace;
        });

        // add hosted services here
        hostBuilder.Services.AddHostedService<HostedDbStartup>();
        hostBuilder.Services.AddHostedService<HostedMatchFinder>();

        // end hosted services
        App host = hostBuilder.Build();

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("host built, running app");

        /*
        TaskScheduler.UnobservedTaskException += (sender, ex) => {
            logger.LogError(ex.Exception, $"unhandled exception");
            Trace.WriteLine(ex);
            ex.SetObserved();
        };
        */

        host.Run();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
            .WithInterFont()
            .ConfigureFonts(manager => {
                manager.AddFontCollection(new FontCollection());
            })
            .LogToTrace(Avalonia.Logging.LogEventLevel.Information);
    }

}
