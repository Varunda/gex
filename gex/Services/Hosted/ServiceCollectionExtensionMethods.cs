using gex.Services.Hosted.BackgroundTasks;
using gex.Services.Hosted.PeriodicTasks;
using gex.Services.Hosted.QueueProcessor;
using gex.Services.Hosted.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Hosted {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppStartupServices(this IServiceCollection services) {
            services.AddHostedService<StorageLocationsWriteCheckStartupService>();
            services.AddHostedService<SevenZipCheckStartupService>();
			services.AddHostedService<StartupTestService>();
            services.AddHostedService<ProcessingQueueStarterService>();
        }

        public static void AddQueueProcessors(this IServiceCollection services) {
            services.AddHostedService<GameReplayDownloadQueueProcessor>();
            services.AddHostedService<GameReplayParseQueueProcessor>();
            services.AddHostedService<HeadlessRunQueueProcessor>();
            services.AddHostedService<ActionLogParseQueueProcessor>();
            services.AddHostedService<UserMapStatUpdateQueueProcessor>();
            services.AddHostedService<UserFactionStatUpdateQueueProcessor>();
			services.AddHostedService<HeadlessRunStatusUpdateQueueProcessor>();
			services.AddHostedService<MapStatUpdateQueueProcessor>();
        }

        public static void AddPeriodicServices(this IServiceCollection services) {
            services.AddHostedService<GameFetcherPeriodicService>();
            services.AddHostedService<GameVersionCleanupPeriodicService>();
        }

		public static void AddBackgroundServices(this IServiceCollection services) {
			services.AddHostedService<PriorityMatchHeadlessBackgroundTask>();
		}

    }
}
