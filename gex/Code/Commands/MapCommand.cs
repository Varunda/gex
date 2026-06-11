using gex.Commands;
using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class MapCommand {

        private readonly ILogger<MapCommand> _Logger;
        private readonly BarMapRepository _MapRepository;
        private readonly MapSymmetryUtil _MapSymmetryUtil;

        public MapCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<MapCommand>>();
            _MapRepository = services.GetRequiredService<BarMapRepository>();
            _MapSymmetryUtil = services.GetRequiredService<MapSymmetryUtil>();
        }

        public Task SymmetryAll() {
            _Logger.LogInformation($"calculating the symmetry for all maps");

            new Task(async () => {
                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(5));
                List<BarMap> maps = await _MapRepository.GetAll(cts.Token);

                _Logger.LogInformation($"updating symmetry for all maps [count={maps.Count}]");

                foreach (BarMap map in maps) {
                    Result<MapSymmetryAxis, string> sym = await _MapSymmetryUtil.Find(map.FileName);
                    if (sym.IsOk == false) {
                        _Logger.LogWarning($"failed to get symmetry for map [map={map.FileName}] [error={sym.Error}]");
                        continue;
                    }

                    map.SymmetryAxis = sym.Value;
                    await _MapRepository.Upsert(map, cts.Token);
                }
            }).Start();

            return Task.CompletedTask;
        }

        public async Task Symmetry(string mapFilename) {
            _Logger.LogInformation($"calculating the symmetry map filename [map={mapFilename}]");

            using CancellationTokenSource cts = new(TimeSpan.FromMinutes(5));
            BarMap? map = await _MapRepository.GetByFileName(mapFilename, cts.Token);

            if (map == null) {
                _Logger.LogWarning($"cannot update map symmetry, no map found [map={mapFilename}]");
                return;
            }

            Result<MapSymmetryAxis, string> sym = await _MapSymmetryUtil.Find(map.FileName);
            if (sym.IsOk == false) {
                _Logger.LogWarning($"failed to get symmetry for map [map={map.FileName}] [error={sym.Error}]");
                return;
            }

            map.SymmetryAxis = sym.Value;
            await _MapRepository.Upsert(map, cts.Token);

            _Logger.LogInformation($"map symmetry for map update [map={mapFilename}] [symmetry={sym.Value}]");
        }

    }
}
