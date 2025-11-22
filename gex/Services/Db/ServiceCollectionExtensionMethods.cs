using gex.Services.Db.Account;
using gex.Services.Db.Event;
using gex.Services.Db.MapStats;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Db {

    public static class ServiceCollectionExtensionMethods {

        public static void AddDatabasesServices(this IServiceCollection services) {
            // internal
            services.AddSingleton<AppMetadataDbStore>();

            // account
            services.AddSingleton<AppAccountDbStore>();
            services.AddSingleton<AppAccountGroupMembershipDb>();
            services.AddSingleton<AppGroupDb>();
            services.AddSingleton<AppGroupPermissionDb>();

            // bar match
            services.AddSingleton<BarReplayDb>();
            services.AddSingleton<BarMatchDb>();
            services.AddSingleton<BarMatchProcessingDb>();
            services.AddSingleton<BarMatchProcessingPriorityDb>();
            services.AddSingleton<BarMatchAllyTeamDb>();
            services.AddSingleton<BarMatchPlayerDb>();
            services.AddSingleton<BarMatchSpectatorDb>();
            services.AddSingleton<BarMatchChatMessageDb>();
            services.AddSingleton<BarMatchTeamDeathDb>();

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
            services.AddSingleton<BarUserUnitsMadeDb>();

            // map stats
            services.AddSingleton<MapStatsDb>();
            services.AddSingleton<MapStatsStartSpotDb>();
            services.AddSingleton<MapStatsByFactionDb>();
            services.AddSingleton<MapStatsOpeningLabDb>();
            services.AddSingleton<MapDailyPlaysDb>();

            // other
            services.AddSingleton<BarMapDb>();
            services.AddSingleton<GameVersionUsageDb>();
            services.AddSingleton<MapPriorityModDb>();
            services.AddSingleton<BarSkillLeaderboardDb>();
            services.AddSingleton<SkillHistogramDb>();
            services.AddSingleton<BarMapPlayCountDb>();
            services.AddSingleton<UnitTweakPriorityExemptionDb>();
            services.AddSingleton<DiscordSubscriptionMatchProcessedDb>();
            services.AddSingleton<DiscordBarUserLinkDb>();
            services.AddSingleton<LobbyAlertDb>();
            services.AddSingleton<MatchPoolDb>();
            services.AddSingleton<MatchPoolEntryDb>();
            services.AddSingleton<BadGameVersionDb>();
            services.AddSingleton<MapEngineUsageDb>();
        }

    }

}