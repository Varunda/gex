using gex.Services.Repositories.Account;
using gex.Services.Repositories.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Repositories {

    public static class IServiceCollectionExtensionMethods {

        public static void AddGexRepositories(this IServiceCollection services) {
            services.AddSingleton<BarReplayFileRepository>();
            services.AddSingleton<BarMatchRepository>();
            services.AddSingleton<BarMatchPlayerRepository>();
            services.AddSingleton<BarMapRepository>();
            services.AddSingleton<BarMatchProcessingRepository>();
            services.AddSingleton<HeadlessRunStatusRepository>();

            services.AddSingleton<AppPermissionRepository>();
            services.AddSingleton<AppGroupRepository>();
            services.AddSingleton<AppAccountGroupMembershipRepository>();
            services.AddSingleton<BarSkillLeaderboardRepository>();
            services.AddSingleton<SkillHistogramRepository>();
            services.AddSingleton<BarMapPlayCountRepository>();
            services.AddSingleton<MapStatsStartSpotRepository>();
            services.AddSingleton<TeiServerRepository>();
            services.AddSingleton<IGithubDownloadRepository, GithubDownloadRepository>();
            services.AddSingleton<BarWeaponDefinitionRepository>();
            services.AddSingleton<BarUnitRepository>();
            services.AddSingleton<BarI18nRepository>();
            services.AddSingleton<MatchPoolRepository>();

            // user stats
            services.AddSingleton<BarUserRepository>();
            services.AddSingleton<BarUserUnitsMadeRepository>();
        }

    }
}
