using gex.Services.Db.Event;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
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
            services.AddSingleton<GameEventExtraStatsDb>();
            services.AddSingleton<GameEventWindUpdateDb>();
            services.AddSingleton<GameEventCommanderPositionUpdateDb>();
            services.AddSingleton<GameEventFactoryUnitCreatedDb>();
            services.AddSingleton<GameEventUnitGivenDb>();
            services.AddSingleton<GameEventUnitTakenDb>();
            services.AddSingleton<GameEventTransportLoadedDb>();
            services.AddSingleton<GameEventTransportUnloadedDb>();
            services.AddSingleton<GameEventTeamDiedDb>();
            services.AddSingleton<GameEventUnitResourcesDb>();
            services.AddSingleton<GameEventUnitDamageDb>();
			services.AddSingleton<GameEventUnitPositionDb>();

            // user stats
            services.AddSingleton<BarUserDb>();
            services.AddSingleton<BarUserSkillDb>();
            services.AddSingleton<BarUserMapStatsDb>();
            services.AddSingleton<BarUserFactionStatsDb>();

            // other
            services.AddSingleton<BarMapDb>();
            services.AddSingleton<GameVersionUsageDb>();
        }

    }

}