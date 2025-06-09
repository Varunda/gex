using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace gex.Code.Hubs.Implementations {

    public class WorldOverviewHub : Hub<IWorldOverviewHub> {

        private readonly ILogger<WorldOverviewHub> _Logger;

        public WorldOverviewHub(ILogger<WorldOverviewHub> logger) {

            _Logger = logger;
        }

        public override async Task OnConnectedAsync() {
            _Logger.LogInformation($"New connection: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

    }
}
