using gex.Common.Code;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Common.Services;
using gex.Common.Services.Bar;
using gex.Common.Services.Lobby;
using gex.Common.Services.Lobby.Implementations;
using gex.Familiar.Models.Options;
using gex.Familiar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace gex.Familiar {

    public class Program {

        private readonly IHost _Host;

        [NotNull]
        public static IServiceProvider Services = default!;

        public static void Main(string[] args) {
            Program program = new(args);
        }

        public Program(string[] args) {
            _Host = Host.CreateDefaultBuilder()
                .ConfigureServices((HostBuilderContext ctx, IServiceCollection services) => {
                    ConfigureServices(services, ctx.Configuration);
                })
                .ConfigureAppConfiguration((IConfigurationBuilder ctx) => {
                    ctx.AddJsonFile("appsettings.json");
                    ctx.AddJsonFile("secrets.json");
                    ctx.AddJsonFile("env.json");
                })
                .ConfigureLogging((ILoggingBuilder logging) => {
                    logging.AddConsole(options => options.FormatterName = "OneLineLogger")
                        .AddConsoleFormatter<OneLineLogger, AppFormatterOptions>(options => { });
                })
                .Build();

            _Host.Run();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration config) {
            Console.WriteLine($"configuring services");
            services.AddLogging();

            services.Configure<SpringFamiliarOptions>(config.GetSection("Spring"));
            services.Configure<JwtFamiliarOptions>(config.GetSection("Jwt"));
            services.Configure<FileStorageOptions>(config.GetSection("FileStorage"));
            services.Configure<FamiliarInstanceOptions>(config.GetSection("Instance"));

            services.AddSingleton<LobbyManager>();
            services.AddSingleton<ILobbyClient, LobbyClient>();
            services.AddSingleton<FamiliarHubClient>();
            services.AddSingleton<StatusHolder>();
            services.AddSingleton<BarEngineDownloader>();
            services.AddSingleton<PrDownloaderService>();
            services.AddSingleton<EnginePathUtil>();
            services.AddSingleton<PathEnvironmentService>();
            services.AddSingleton<GameInstance>();
            services.AddSingleton<MatchUploader>();

            services.AddHostedService<SpringLobbyFamiliarHost>();
            services.AddHostedService<CoordinatorConnectionHost>();
        }

    }
}
