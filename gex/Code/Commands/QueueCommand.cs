using gex.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace gex.Code.Commands {

    [Command]
    public class QueueCommand {

        private readonly ILogger<QueueCommand> _Logger;

        public QueueCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<QueueCommand>>();
        }

    }
}
