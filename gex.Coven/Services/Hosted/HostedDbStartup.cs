using gex.Common.Services.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Hosted {

    public class HostedDbStartup : IHostedService {

        private readonly ILogger<HostedDbStartup> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDbCreator _DbCreator;

        public HostedDbStartup(ILogger<HostedDbStartup> logger,
            IDbHelper dbHelper, IDbCreator dbCreator) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _DbCreator = dbCreator;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            // db creation
            _Logger.LogDebug("starting DB creation");
            Stopwatch timer = Stopwatch.StartNew();
            await _DbCreator.Execute();
            _Logger.LogDebug($"DB creation done [timer={timer.ElapsedMilliseconds}ms]");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }
}
