using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Db {

    public static class ServiceCollectionExtensionMethods {

        public static void AddDatabasesServices(this IServiceCollection services) {
            services.AddSingleton<AppAccountDbStore>();
            services.AddSingleton<AppMetadataDbStore>();

            // bar match
            services.AddSingleton<BarReplayDb>();
            services.AddSingleton<BarMatchDb>();
            services.AddSingleton<BarMatchProcessingDb>();
            services.AddSingleton<BarMatchAllyTeamDb>();
            services.AddSingleton<BarMatchPlayerDb>();
            services.AddSingleton<BarMatchSpectatorDb>();
            services.AddSingleton<BarMatchChatMessageDb>();

            // game event
            services.AddSingleton<GameEventUnitCreatedDb>();
            services.AddSingleton<GameEventUnitKilledDb>();
            services.AddSingleton<GameEventUnitDefDb>();
            services.AddSingleton<UnitSetToGameIdDb>();
            services.AddSingleton<GameEventTeamStatsDb>();
        }

    }

}