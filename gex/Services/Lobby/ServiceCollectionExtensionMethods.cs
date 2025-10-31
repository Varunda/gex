using gex.Common.Services.Lobby;
using gex.Common.Services.Lobby.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Lobby {

    public static class ServiceCollectionExtensionMethods {

        public static void AddLobbyServices(this IServiceCollection services, bool enabled) {
            if (enabled) {
                services.AddSingleton<ILobbyClient, LobbyClient>();
            } else {
                services.AddSingleton<ILobbyClient, EmptyLobbyClient>();
            }

            services.AddSingleton<gex.Common.Services.Lobby.LobbyManager>();
            services.AddSingleton<FamiliarCoordinator>();
        }

    }
}
