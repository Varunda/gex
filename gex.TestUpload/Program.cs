using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace gex.TestUpload {

    public class Program {

        public static void Main(string[] args) {

            HostApplicationBuilderSettings settings = new() {
                Args = args,
                Configuration = new ConfigurationManager(),
                ContentRootPath = Directory.GetCurrentDirectory()
            };

            settings.Configuration.AddJsonFile("env.json");
            settings.Configuration.AddJsonFile("secrets.json");

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);
            builder.Services.Configure<Env>(builder.Configuration.GetSection("Env"));
            builder.Services.Configure<Secret>(builder.Configuration.GetSection("Secret"));

            using IHost host = builder.Build();
            host.Run();
        }

    }

    public class AppHost : IHostedService {

        private readonly ILogger<AppHost> _Logger;

        public readonly Uploader _Uploader;

        public AppHost(ILogger<AppHost> logger,
            Uploader uploader) {

            _Logger = logger;
            _Uploader = uploader;
        }

        public async Task StartAsync(CancellationToken cancel) {
            _Logger.LogInformation("started");
            await Run(cancel);
        }

        public Task StopAsync(CancellationToken cancel) {
            return Task.CompletedTask;
        }

        private async Task Run(CancellationToken cancel) {


        }

    }

}
