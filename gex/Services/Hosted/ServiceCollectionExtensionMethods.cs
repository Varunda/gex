using gex.Services.Hosted.BackgroundTasks;
using gex.Services.Hosted.PeriodicTasks;
using gex.Services.Hosted.QueueProcessor;
using gex.Services.Hosted.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Hosted {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppStartupServices(this IServiceCollection services) {
            services.AddHostedService<ServiceEnableStartupService>();
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
            services.AddHostedService<SubscriptionMessageQueueProcessor>();
            services.AddHostedService<FixCountryCodeQueueProcessor>();

            services.AddHostedService<UserMapStatUpdateQueueProcessor>();
            services.AddHostedService<UserFactionStatUpdateQueueProcessor>();
            services.AddHostedService<HeadlessRunStatusUpdateQueueProcessor>();
            services.AddHostedService<MapStatUpdateQueueProcessor>();
            services.AddHostedService<LobbyMessageQueueProcessor>();
            services.AddHostedService<BattleStatusQueueProcessor>();
        }

        public static void AddPeriodicServices(this IServiceCollection services) {
            services.AddHostedService<GameFetcherPeriodicService>();
            services.AddHostedService<GameVersionCleanupPeriodicService>();
            services.AddHostedService<LobbyAlertSendingPeriodicService>();
            services.AddHostedService<GitHubUnitDataUpdatePeriodicService>();
            services.AddHostedService<LobbyBattleStatusApiUpdatePeriodicService>();
        }

        public static void AddBackgroundServices(this IServiceCollection services) {
            services.AddHostedService<PriorityMatchHeadlessBackgroundTask>();
            services.AddHostedService<SpringLobbyClientHost>();
            services.AddHostedService<UnitPositionCompressionService>();
        }

    }
}
