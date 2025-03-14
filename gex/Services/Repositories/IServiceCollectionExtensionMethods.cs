using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Repositories {

    public static class IServiceCollectionExtensionMethods {

        public static void AddGexRepositories(this IServiceCollection services) {
            services.AddSingleton<BarReplayFileRepository>();
            services.AddSingleton<BarMatchRepository>();
            services.AddSingleton<BarMatchPlayerRepository>();
        }

    }
}
