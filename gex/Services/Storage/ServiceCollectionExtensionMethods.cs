using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Storage {

    public static class ServiceCollectionExtensionMethods {

        public static void AddStorageServices(this IServiceCollection services) {
            services.AddSingleton<GameOutputStorage>();
            services.AddSingleton<UnitPositionFileStorage>();
            services.AddSingleton<DemofileStorage>();
        }

    }
}
