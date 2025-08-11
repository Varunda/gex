using gex.Models.Api;
using gex.Models.Discord;
using gex.Models.Lobby;
using gex.Models.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Queues {

    public static class ServiceCollectionExtentionMethods {

        /// <summary>
        ///     And the various queues gex uses
        /// </summary>
        /// <param name="services">Extension instance</param>
        public static void AddGexQueueServices(this IServiceCollection services) {
            services.AddSingleton<BaseQueue<AppDiscordMessage>, DiscordMessageQueue>();
            services.AddSingleton<BaseQueue<GameReplayDownloadQueueEntry>, GameReplayDownloaderQueue>();
            services.AddSingleton<BaseQueue<GameReplayParseQueueEntry>, GameReplayParseQueue>();
            services.AddSingleton<BaseQueue<HeadlessRunQueueEntry>, HeadlessRunQueue>();
            services.AddSingleton<BaseQueue<ActionLogParseQueueEntry>, ActionLogParseQueue>();
            services.AddSingleton<BaseQueue<UserMapStatUpdateQueueEntry>, UserMapStatUpdateQueue>();
            services.AddSingleton<BaseQueue<UserFactionStatUpdateQueueEntry>, UserFactionStatUpdateQueue>();
            services.AddSingleton<BaseQueue<HeadlessRunStatus>, HeadlessRunStatusUpdateQueue>();
            services.AddSingleton<BaseQueue<MapStatUpdateQueueEntry>, MapStatUpdateQueue>();
            services.AddSingleton<BaseQueue<SubscriptionMessageQueueEntry>, SubscriptionMessageQueue>();
            services.AddSingleton<BaseQueue<LobbyMessage>, LobbyMessageQueue>();
        }

    }
}
