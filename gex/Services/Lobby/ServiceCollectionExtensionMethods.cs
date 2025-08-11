using gex.Services.Lobby.Implementations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Lobby {

    public static class ServiceCollectionExtensionMethods {

        public static void AddLobbyServices(this IServiceCollection services, bool enabled) {
            if (enabled) {
                services.AddSingleton<ILobbyClient, LobbyClient>();
            } else {
                services.AddSingleton<ILobbyClient, EmptyLobbyClient>();
            }

            services.AddSingleton<LobbyManager>();
        }

    }
}
