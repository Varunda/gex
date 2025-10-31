using gex.Common.Models.Familiar;
using gex.Services;
using gex.Services.Lobby;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Hubs.Implementations {

    public class FamiliarHub : Hub<IFamiliarHub> {

        private readonly ILogger<FamiliarHub> _Logger;
        private readonly ICurrentAccount _CurrentUser;
        private readonly FamiliarCoordinator _Familiars;

        public FamiliarHub(ILogger<FamiliarHub> logger,
            ICurrentAccount currentUser, FamiliarCoordinator familiars) {

            _Logger = logger;
            _CurrentUser = currentUser;
            _Familiars = familiars;
        }

        public override async Task OnConnectedAsync() {
            if (Context.User == null) {
                _Logger.LogWarning($"connection attempt from unauthenticated user");
                Context.Abort();
                return;
            }

            string? familiarName = await _CurrentUser.GetClaim("familiar", CancellationToken.None);
            if (familiarName == null) {
                string claimNames = string.Join(", ", Context.User.Claims.Select(iter => iter.Type));
                _Logger.LogWarning($"connection attempt from unauthorized user, missing familiar claim [claims={claimNames}]");
                Context.Abort();
                return;
            }

            _Logger.LogDebug($"hello familiar, welcome back [name={familiarName}]");
            await Clients.Caller.Hello(familiarName);

            try {
                string gexWidget = File.ReadAllText("./gex.lua");
                string byar = File.ReadAllText("./BYAR.lua");
                await Clients.Caller.SendLua(new FamiliarSendWidgetMessage() {
                    Code = gexWidget,
                    Byar = byar
                });
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to send lua to familiar [familiar={familiarName}]");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            if (Context.User == null) {
                return;
            }

            string? familiarName = await _CurrentUser.GetClaim("familiar", CancellationToken.None);
            if (familiarName == null) {
                return;
            }

            _Logger.LogDebug($"farewell familiar [name={familiarName}]");

            await base.OnDisconnectedAsync(exception);
        }

        public Task StatusUpdate(FamiliarStatus status) {
            if (Context.UserIdentifier == null) {
                Context.Abort();
                return Task.CompletedTask;
            }

            status.Name = Context.UserIdentifier;
            _Familiars.UpdateStatus(status);
            _Logger.LogDebug($"got status update [user={status.Name}]");
            return Task.CompletedTask;
        }

    }

}
