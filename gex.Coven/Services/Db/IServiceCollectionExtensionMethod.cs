using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Db.Readers;
using gex.Coven.Services.Db.Match;
using gex.Coven.Services.Db.Reader;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db {

    public static class IServiceCollectionExtensionMethod {

        public static void AddCovenDbServices(this IServiceCollection services) {

            services.AddSingleton<IDbHelper, SqLiteDbHelper>();
            services.AddSingleton<IDbCreator, SqLiteDbCreator>();

            // match
            services.AddSingleton<IBarMatchDb, SqLiteBarMatchDb>();
            services.AddSingleton<IBarMatchProcessingDb, SqLiteBarMatchProcessingDb>();
            services.AddSingleton<IBarMatchTeamDb, SqLiteBarMatchTeamDb>();
            services.AddSingleton<IBarMatchAllyTeamDb, SqLiteBarMatchAllyTeamDb>();
            services.AddSingleton<IBarMatchPlayerDb, SqLiteBarMatchPlayerDb>();
            services.AddSingleton<IBarMatchSpectatorDb, SqLiteBarMatchSpectatorDb>();
            services.AddSingleton<IBarMatchChatMessageDb, SqLiteBarMatchChatMessageDb>();
            services.AddSingleton<IBarMatchTeamDeathDb, SqLiteBarMatchTeamDeathDb>();
            services.AddSingleton<IBarMatchPlayerLeftDb, SqLiteBarMatchPlayerLeftDb>();
            services.AddSingleton<IBarMatchTextPingDb, SqLiteBarMatchTextPingDb>();
            services.AddSingleton<IBarMatchAiPlayerDb, SqLiteBarMatchAiPlayerDb>();
            services.AddSingleton<BarMatchHashDb>();
            services.AddSingleton<BarMatchIgnoredFilesDb>();

            // other
            services.AddSingleton<IBarMapDb, SqLiteBarMapDb>();

            // readers
            services.AddSingleton<IDataReader<BarMatch>, BarMatchDbReader>();
            services.AddSingleton<IDataReader<BarMatchTeam>, BarMatchTeamDbReader>();
            services.AddSingleton<IDataReader<BarMatchAllyTeam>, BarMatchAllyTeamDbReader>();

        }

    }
}
