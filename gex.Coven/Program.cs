using Avalonia;
using Avalonia.Logging;
using gex.Common.Services;
using gex.Common.Services.Db;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Code;
using gex.Coven.Code.ExtensionMethods;
using gex.Coven.Models.Config;
using gex.Coven.Services;
using gex.Coven.Services.Bar;
using gex.Coven.Services.Db;
using gex.Coven.Services.Hosted;
using gex.Coven.Services.Util;
using gex.Coven.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using NReco.Logging.File;
using R86.Avalonia.Hosting;
using System;
using System.Diagnostics;
using System.IO;
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

        if (args.Length > 0) {
            PendingDemofileRead = args[0];
        }

        HostedApplication<App>.AvaloniaApplicationBuilder hostBuilder
            = App.CreateBuilder(args, BuildAvaloniaApp, () => Host.CreateEmptyApplicationBuilder(null));

        hostBuilder.Configuration
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json")
            .AddInMemoryCollection();

        hostBuilder.Services.AddMemoryCache();

        // services
        hostBuilder.Services.AddSingleton<LuaRunner>();
        hostBuilder.Services.AddSingleton<BarDemofileParser>();
        hostBuilder.Services.AddSingleton<ActionLogParser>();
        hostBuilder.Services.AddSingleton<StartSpotDataParser>();
        hostBuilder.Services.AddSingleton<BarMapParser>();
        hostBuilder.Services.AddSingleton<MatchListViewModel>();
        hostBuilder.Services.AddSingleton<ToastService>();
        hostBuilder.Services.AddSingleton<DisplayLoggerService>();
        hostBuilder.Services.AddSingleton<UserOptionsViewModel>();
        hostBuilder.Services.AddSingleton<DemofileWatcher>();
        hostBuilder.Services.AddSingleton<LuaCommandParser>();
        hostBuilder.Services.AddSingleton<PolygonStartboxUtil>();
        hostBuilder.Services.AddSingleton<BarMatchRepository>();
        hostBuilder.Services.AddSingleton<UserOptionsService>();
        hostBuilder.Services.AddSingleton<BarMatchPlayerRepository>();
        hostBuilder.Services.AddSingleton<BarMatchTeamRepository>();

        hostBuilder.Services.AddCovenDbServices();
        hostBuilder.Services.AddCovenUtils();
        hostBuilder.Services.AddCovenBarServices();

        // logging
        hostBuilder.Services.AddLogging(builder => {
            builder.AddConfiguration(hostBuilder.Configuration.GetSection("Logging"));
            builder.AddDisplayLogger();

            builder.AddFile(Path.GetDirectoryName(Environment.ProcessPath) + "/logs/gex.Coven-{0:yyyy}-{0:MM}-{0:dd}.log", (FileLoggerOptions options) => {
                options.FormatLogFileName = fName => {
                    return string.Format(fName, DateTime.UtcNow);
                };
                options.FileSizeLimitBytes = (1024 * 1024 * 64); // 64MB
                options.MaxRollingFiles = 10;
                options.MinLevel = LogLevel.Trace;
            });
        });

        // add hosted services here
        hostBuilder.Services.AddHostedService<HostedDbStartup>();

        // end hosted services
        App host = hostBuilder.Build();

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        char ps = Path.DirectorySeparatorChar;
        logger.LogInformation($"host built, running app [args={string.Join(' ', args)}] [cwd={Environment.CurrentDirectory}] "
            + $"[cmd={Environment.CommandLine}] [process path={Environment.ProcessPath}] [user={ps}{Environment.UserName}{ps} (supposed to be redacted)]");

        TaskScheduler.UnobservedTaskException += (sender, e) => {
            logger.LogError(e.Exception, $"unobserved task exception");
        };

        try {
            host.Run();
        } catch (Exception ex) {
            logger.LogError(ex, $"error in Run() for host");
        }
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
            .LogToTrace(LogEventLevel.Information)
            .LogToTrace(LogEventLevel.Verbose, LogArea.Binding);
    }

    /// <summary>
    ///     when launched by double clicking a sdfz file, this will contain the full path
    ///     of the demofile to be launched and loaded
    /// </summary>
    public static string? PendingDemofileRead { get; private set; } = null;

}
