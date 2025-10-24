using gex.Common.Models;
using gex.Models.Bar;
using gex.Models;
using gex.Models.Options;
using gex.Services.Db.Event;
using gex.Services.Lobby;
using gex.Services.Lobby.Implementations;
using gex.Services.Parser;
using gex.Services.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class StartupTestService : BackgroundService {

        private readonly ILogger<StartupTestService> _Logger;
        private readonly IGithubDownloadRepository _UserGithubRepository;
        private readonly GameEventUnitDefDb _UnitDefDb;
        private readonly BarUnitParser _UnitParser;
        private readonly IOptions<FileStorageOptions> _FileOptions;

        public StartupTestService(ILogger<StartupTestService> logger,
            IGithubDownloadRepository userGithubRepository, GameEventUnitDefDb unitDefDb,
            BarUnitParser unitParser, IOptions<FileStorageOptions> fileOptions) {

            _Logger = logger;
            _UserGithubRepository = userGithubRepository;
            _UnitDefDb = unitDefDb;
            _UnitParser = unitParser;
            _FileOptions = fileOptions;
        }

        protected override Task ExecuteAsync(CancellationToken cancel) {
            Task.Run(async () => {
                try {
                    await Task.Delay(TimeSpan.FromMilliseconds(1), cancel);

                    /*
                    List<string> unitDefs = (await _UnitDefDb.GetUnitNames(cancel)).Aggregate(new List<string>(), (acc, iter) => {
                        acc.AddRange(iter.DefinitionNames);
                        return acc;
                    });

                    foreach (string unitDef in unitDefs) {
                        if (unitDef == "dummycom") {
                            continue;
                        }

                        string path = Path.Join(_FileOptions.Value.UnitDataLocation, $"{unitDef}.lua");
                        if (File.Exists(path) == false) {
                            continue;
                        }

                        string c = await File.ReadAllTextAsync(path);

                        Result<BarUnit, string> res = await _UnitParser.Parse(c, cancel);
                        if (res.IsOk == false) {
                            _Logger.LogWarning($"failed to parse unit [unitDef={unitDef}] [error={res.Error}]");
                        }
                    }
                    */
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"exception in github download");
                }
            }, cancel);

            return Task.CompletedTask;
        }

    }
}
