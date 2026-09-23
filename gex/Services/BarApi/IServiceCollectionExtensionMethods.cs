using gex.Common.Services.Bar;
using gex.Common.Services.Parser;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.BarApi {

    public static class IServiceCollectionExtensionMethods {

        public static void AddBarApiServices(this IServiceCollection services) {
            services.AddSingleton<BarReplayApi>();
            services.AddSingleton<BarReplayFileApi>();
            services.AddSingleton<HeadlessPrDownloaderService>();
            services.AddSingleton<HeadlessBarEngineDownloader>();
            services.AddSingleton<BarHeadlessInstance>();
            services.AddSingleton<ActionLogParser>();
            services.AddSingleton<BarMapApi>();
            services.AddSingleton<TeiServerApi>();
            services.AddSingleton<BarBattleStatusApi>();
        }

    }
}
