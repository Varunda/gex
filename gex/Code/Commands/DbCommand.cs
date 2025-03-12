using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;

namespace gex.Commands {

    [Command]
    public class DbCommand {

        private readonly ILogger<DbCommand> _Logger;

        public DbCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<DbCommand>>();
        }

        public void FlushPool() {
            _Logger.LogInformation($"Flushing pools...");
            NpgsqlConnection.ClearAllPools();
            _Logger.LogInformation($"Flushed all pools");
        }

    }

}