using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using gex.Commands;
using gex.Models;
using gex.Services.Db;

namespace gex.Code.Commands {

    [Command]
    public class AccountCommand {

        private readonly ILogger<AccountCommand> _Logger;
        private readonly AppAccountDbStore _AccountDb;

        public AccountCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<AccountCommand>>();
            _AccountDb = services.GetRequiredService<AppAccountDbStore>();
        }

        public async Task Create(string name, ulong discordID) {
            AppAccount account = new AppAccount() {
                Name = name,
                DiscordID = discordID
            };

            long ID = await _AccountDb.Insert(account, CancellationToken.None);
            _Logger.LogInformation($"Created new account {ID} for {name}");
        }

    }
}
