using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Parser {

    public static class IServiceCollectionExtensionMethod {

        public static void AddGexParsers(this IServiceCollection services) {
            services.AddSingleton<BarDemofileParser>();
            services.AddSingleton<BarMapParser>();
            services.AddSingleton<BarUnitParser>();
            services.AddSingleton<BarWeaponDefinitionParser>();
        }

    }
}
