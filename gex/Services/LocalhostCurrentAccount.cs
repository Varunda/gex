using DSharpPlus.SlashCommands;
using gex.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services {

    public class LocalhostCurrentAccount : ICurrentAccount {

        private readonly ILogger<LocalhostCurrentAccount> _Logger;

        private static DateTime _LastReminder = DateTime.MinValue;

        public LocalhostCurrentAccount(ILogger<LocalhostCurrentAccount> logger) {
            _Logger = logger;
        }

        public Task<AppAccount?> Get(CancellationToken cancel = default) {
            if (DateTime.UtcNow - _LastReminder >= TimeSpan.FromMinutes(3)) {
                _Logger.LogInformation($"reminder: localhost developer authentication is disabled. all permissions checks will pass with all users (3m cooldown)");
                _LastReminder = DateTime.UtcNow;
            }

            // accounts with ID of 1 are considered root users and have all permissions, even if not in any groups
            return Task.FromResult((AppAccount?) new AppAccount() {
                ID = AppAccount.Root,
                Name = "localhost dev",
                DiscordID = 0uL,
            });
        }

        public Task<string?> GetClaim(string claimType, CancellationToken cancel = default) {
            throw new System.NotImplementedException();
        }

        public Task<AppAccount?> GetDiscord(BaseContext ctx) {
            throw new System.NotImplementedException();
        }

        public Task<ulong?> GetDiscordID(CancellationToken cancel = default) {
            throw new System.NotImplementedException();
        }

    }
}
