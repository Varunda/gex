using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.BarApi {

    public static class IServiceCollectionExtensionMethods {

        public static void AddBarApiServices(this IServiceCollection services) {
            services.AddSingleton<BarReplayApi>();
            services.AddSingleton<BarReplayFileApi>();
            services.AddSingleton<PrDownloaderService>();
            services.AddSingleton<BarEngineDownloader>();
            services.AddSingleton<BarHeadlessInstance>();
            services.AddSingleton<ActionLogParser>();
            services.AddSingleton<BarMapApi>();
            services.AddSingleton<TeiServerApi>();
        }

    }
}
