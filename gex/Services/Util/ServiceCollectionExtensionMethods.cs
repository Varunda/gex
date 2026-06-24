using gex.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Util {

    public static class ServiceCollectionExtensionMethods {

        public static void AddUtilServices(this IServiceCollection services) {
            services.AddSingleton<ApmCalculatorUtil>();
            services.AddSingleton<BarDemofileResultProcessor>();
            services.AddSingleton<BarMatchPriorityCalculator>();
            services.AddSingleton<BarMatchTitleUtilService>();
            services.AddSingleton<MapSymmetryUtil>();
            services.AddSingleton<SafeZLib>();
            services.AddSingleton<EnginePathUtil>();
        }

    }
}
