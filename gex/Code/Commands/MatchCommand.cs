using gex.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class MatchCommand {

        private readonly ILogger<MatchCommand> _Logger;

        private static string _PendingGameDelete = "";

        public MatchCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<MatchCommand>>();
        }

        public async Task Delete(string gameID) {

        }


    }
}
