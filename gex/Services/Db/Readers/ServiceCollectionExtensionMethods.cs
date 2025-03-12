using Microsoft.Extensions.DependencyInjection;
using gex.Services.Db;
using gex.Services.Db.Readers;
using gex.Models.Db;
using gex.Models;

namespace gex.Services.Db.Readers {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppDatabaseReadersServices(this IServiceCollection services) {
            services.AddSingleton<IDataReader<AppAccount>, AppAccountReader>();
            services.AddSingleton<IDataReader<BarMatch>, BarMatchDbReader>();
            services.AddSingleton<IDataReader<BarMatchAllyTeam>, BarMatchAllyTeamDbReader>();
            services.AddSingleton<IDataReader<BarMatchPlayer>, BarMatchPlayerDbReader>();
            services.AddSingleton<IDataReader<BarMatchSpectator>, BarMatchSpectatorDbReader>();
            services.AddSingleton<IDataReader<BarMatchChatMessage>, BarMatchChatMessageDbReader>();
        }

    }

}