using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Metrics {

	public static class ServiceCollectionExtensionMethods {

        public static void AddGexMetrics(this IServiceCollection services) {
            // don't forget to add the meter name attribute in the metric service itself!
            services.AddSingleton<QueueMetric>();
			services.AddSingleton<BarApiMetric>();
        }

	}
}
