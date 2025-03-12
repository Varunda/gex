using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using gex.Commands;
using gex.Models.Queues;
using gex.Services.Queues;

namespace gex.Code.Commands {

    [Command]
    public class QueueCommand {

        private readonly ILogger<QueueCommand> _Logger;

        public QueueCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<QueueCommand>>();
        }

    }
}
