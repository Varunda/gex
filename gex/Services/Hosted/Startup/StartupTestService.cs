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
        private readonly ActionLogParser _ActionLogParser;
        private readonly GameEventUnitCreatedDb _UnitCreatedDb;
        private readonly GameEventUnitKilledDb _UnitKilledDb;

        public StartupTestService(ILogger<StartupTestService> logger,
            ActionLogParser actionLogParser, GameEventUnitCreatedDb unitCreatedDb,
            GameEventUnitKilledDb unitKilledDb) {

            _Logger = logger;
            _ActionLogParser = actionLogParser;
            _UnitCreatedDb = unitCreatedDb;
            _UnitKilledDb = unitKilledDb;
        }

        protected override Task ExecuteAsync(CancellationToken cancel) {

            return Task.Run(async () => {

                Result<GameOutput, string> game = await _ActionLogParser.Parse("8d66cf67f02f9d4ddadd5d51e8042fab", "E:/Gex/GameLogs/8d66cf67f02f9d4ddadd5d51e8042fab/actions.json", cancel);
                if (game.IsOk == false) {
                    return;
                }

                foreach (GameEventUnitKilled ev in game.Value.UnitsKilled) {
                    await _UnitKilledDb.Insert(ev);
                }

                List<GameEventUnitKilled> k = await _UnitKilledDb.GetByGameID(game.Value.GameID);

                //await _BarEngineDownloader.DownloadEngine("2025.01.6", cancel);
                //await _PrDownloader.GetGameVersion("2025.01.6", "Beyond All Reason test-27562-33e445c", cancel);
                //await _PrDownloader.GetMap("2025.01.6", "Isidis crack 1.1", cancel);

                //await _HeadlessRunner.RunGame("6834cf672f1479b27a6d8010836e609a", cancel);
            }, cancel);
        }

    }
}
