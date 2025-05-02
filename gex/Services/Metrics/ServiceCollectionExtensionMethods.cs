using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Metrics {

	public static class ServiceCollectionExtensionMethods {

        public static void AddGexMetrics(this IServiceCollection services) {
            // don't forget to add the meter name to Program.cs too!
            services.AddSingleton<QueueMetric>();
        }

	}
}
