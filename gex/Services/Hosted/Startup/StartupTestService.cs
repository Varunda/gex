using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Event;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Demofile;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class StartupTestService : BackgroundService {

        private readonly ILogger<StartupTestService> _Logger;
        private readonly BarMapApi _MapApi;
        private readonly BarMapDb _MapDb;

        public StartupTestService(ILogger<StartupTestService> logger,
            BarMapApi mapApi, BarMapDb mapDb) {

            _Logger = logger;
            _MapApi = mapApi;
            _MapDb = mapDb;
        }

        protected override Task ExecuteAsync(CancellationToken cancel) {
            return Task.Run(async () => {

                Result<BarMap, string> api = await _MapApi.GetByName("isidis_crack_1.1", cancel);
                if (api.IsOk == false) {
                    _Logger.LogError($"failed to get map [error={api.Error}]");
                    return;
                }

                await _MapDb.Upsert(api.Value);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }, cancel);
        }

    }
}
