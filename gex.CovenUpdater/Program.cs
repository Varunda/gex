using gex.Common.Code;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Common.Services.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NReco.Logging.File;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace gex.CovenUpdater {

    public class Program {

        private readonly IHost _Host;

        [NotNull]
        public static IServiceProvider Services = default!;

        public static void Main(string[] args) {
            Program program = new(args);
        }

        public Program(string[] args) {
            _Host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((HostBuilderContext ctx, IServiceCollection services) => {
                    ConfigureServices(services, ctx.Configuration);
                })
                .ConfigureAppConfiguration((IConfigurationBuilder ctx) => {
                    ctx.AddCommandLine(args);
                })
                .ConfigureLogging((ILoggingBuilder logging) => {

                    logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Error);
                    logging.AddFilter("gex", LogLevel.Trace);
                    logging.AddFilter("gex.Common", LogLevel.Trace);
                    logging.AddFilter("gex.CovenUpdater", LogLevel.Trace);

                    logging.AddConsole(options => options.FormatterName = "OneLineLogger")
                        .AddConsoleFormatter<OneLineLogger, AppFormatterOptions>(options => { });

                    logging.AddFile("update-logs/gex.CovenUpdater-{0:yyyy}-{0:MM}-{0:dd}.log", (FileLoggerOptions options) => {
                        options.FormatLogFileName = fName => {
                            return string.Format(fName, DateTime.UtcNow);
                        };
                        options.FileSizeLimitBytes = (1024 * 1024 * 64); // 64MB
                        options.MaxRollingFiles = 10;
                        options.FilterLogEntry = (msg) => {
                            return msg.LogName != "Microsoft.Hosting.Lifetime";
                        };
                        options.MinLevel = LogLevel.Trace;
                    });
                })
                .Build();

            _Host.Run();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration config) {
            services.AddLogging();

            services.AddSingleton<CovenVersionUtil>();

            services.Configure<CovenRepositoryOptions>(config.GetSection("Repository"));

            services.AddHostedService<App>();
        }

        public class App : IHostedService {

            private readonly ILogger<App> _Logger;
            private readonly CovenVersionUtil _VersionUtil;
            private readonly IHostApplicationLifetime _ApplicationLifetime;
            private readonly IOptions<CovenRepositoryOptions> _Options;

            public App(ILogger<App> logger,
                CovenVersionUtil versionUtil, IHostApplicationLifetime applicationLifetime,
                IOptions<CovenRepositoryOptions> options) {

                _Logger = logger;
                _VersionUtil = versionUtil;
                _ApplicationLifetime = applicationLifetime;
                _Options = options;
            }

            public async Task StartAsync(CancellationToken cancel) {
                _Logger.LogInformation($"starting version check");

                do {
                    _Logger.LogDebug($"loading versions [instance={_Options.Value.Instance}]"
                        + $" [owner={_Options.Value.RepositoryOwner}] [name={_Options.Value.RepositoryName}]");

                    string folderPath = Path.GetFullPath(".");
                    if (File.Exists(Path.Join(folderPath, "gex.Coven.exe")) == false
                        && File.Exists(Path.Join(folderPath, "gex.Coven")) == false) {

                        _Logger.LogWarning($"failed to find gex.Coven application in current folder [folder={folderPath}]");
                        break;
                    }

                    string? currentVersion = null;

                    try {
                        FileVersionInfo? fvi = FileVersionInfo.GetVersionInfo(Path.Join(folderPath, "gex.Coven" + (OperatingSystem.IsWindows() ? ".exe" : "")));
                        currentVersion = fvi?.FileVersion;
                        _Logger.LogInformation($"gex.Coven version loaded [version={currentVersion}]");
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to load version of gex.Coven");
                    }

                    Process[] gexCovenProcs = Process.GetProcessesByName("gex.Coven");
                    if (gexCovenProcs.Length > 0) {
                        _Logger.LogWarning($"gex.Coven is currently running, not performing update");
                        break;
                    }

                    Result<CovenVersion, string> res = await _VersionUtil.GetLatest(_Options.Value, cancel);
                    if (res.IsOk == false) {
                        _Logger.LogError($"failed to get latest coven version from repo [error={res.Error}]");
                        break;
                    }

                    if (currentVersion != null && currentVersion == res.Value.Tag) {
                        _Logger.LogWarning($"no update is needed, current version matches remote version [current version={currentVersion}] [remote version={res.Value.Tag}]");
                        break;
                    }

                    _Logger.LogInformation($"got latest version [name={res.Value.Tag}]");

                    string url = "";
                    if (OperatingSystem.IsWindows()) {
                        url = res.Value.WindowsDownload;
                    } else if (OperatingSystem.IsLinux()) {
                        url = res.Value.LinuxDownload;
                    } else {
                        throw new InvalidOperationException($"gex.CovenUpdater only works on windows and linux");
                    }

                    try {
                        if (Directory.Exists("staging")) {
                            _Logger.LogInformation($"deleting staging directory");
                            Directory.Delete("staging", true);
                        }
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to delete staging directory");
                    }

                    _Logger.LogInformation($"downloading zip [url={url}]");

                    try {
                        using HttpClient http = new();
                        HttpResponseMessage response = await http.GetAsync(url, cancel);

                        Directory.CreateDirectory("staging");
                        Stream body = response.Content.ReadAsStream(cancel);

                        ZipFile.ExtractToDirectory(body, Path.Join(".", "staging"));
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to download and extract zip");
                        break;
                    }

                    try {
                        Directory.CreateDirectory("update-backup");
                        Directory.CreateDirectory("backups");
                        File.Copy("gex.db", "update-backup/gex.db");
                        File.Copy("UserOptions.json", "update-backup/UserOptions.json");

                        string backupZipName = $"backups/gex.Coven_backup_{DateTime.UtcNow:yyyyMMdd_hhmmss}Z.zip";
                        using FileStream backupZip = File.OpenWrite(backupZipName);
                        ZipFile.CreateFromDirectory("update-backup", backupZip);
                        _Logger.LogInformation($"created backup zip [path={backupZipName}]");

                        Directory.Delete("update-backup", true);
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to create upgrade backup");
                        break;
                    }

                    try {
                        string[] files = Directory.GetFiles("staging");
                        foreach (string file in files) {
                            string filename = Path.GetFileName(file);
                            _Logger.LogDebug($"copying file out of staging [filename={filename}] [file={file}]");
                            File.Copy(file, Path.Join(".", filename), true);
                        }
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to copy files out of staging");
                        break;
                    }

                    try {
                        //Directory.Delete("staging", true);
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to delete staging directory");
                    }

                    _Logger.LogInformation($"done! [version={res.Value.Tag}]");

                } while (false);

                _ApplicationLifetime.StopApplication();
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        }

    }
}
