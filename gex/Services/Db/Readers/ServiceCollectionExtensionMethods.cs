using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Models.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Db.Readers {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppDatabaseReadersServices(this IServiceCollection services) {
            services.AddSingleton<IDataReader<AppAccount>, AppAccountReader>();
            services.AddSingleton<IDataReader<BarMatch>, BarMatchDbReader>();
            services.AddSingleton<IDataReader<BarMatchAllyTeam>, BarMatchAllyTeamDbReader>();
            services.AddSingleton<IDataReader<BarMatchPlayer>, BarMatchPlayerDbReader>();
            services.AddSingleton<IDataReader<BarMatchSpectator>, BarMatchSpectatorDbReader>();
            services.AddSingleton<IDataReader<BarMatchChatMessage>, BarMatchChatMessageDbReader>();
            services.AddSingleton<IDataReader<BarMatchTeam>, BarMatchTeamDbReader>();
        }

    }

}