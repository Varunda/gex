﻿using gex.Models.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Queues {

    public static class ServiceCollectionExtentionMethods {

        /// <summary>
        ///     And the various queues gex uses
        /// </summary>
        /// <param name="services">Extension instance</param>
        public static void AddGexQueueServices(this IServiceCollection services) {
            services.AddSingleton<DiscordMessageQueue>();
            services.AddSingleton<BaseQueue<GameReplayDownloadQueueEntry>, GameReplayDownloaderQueue>();
            services.AddSingleton<BaseQueue<GameReplayParseQueueEntry>, GameReplayParseQueue>();
            services.AddSingleton<BaseQueue<HeadlessRunQueueEntry>, HeadlessRunQueue>();
            services.AddSingleton<BaseQueue<ActionLogParseQueueEntry>, ActionLogParseQueue>();
        }

    }
}
