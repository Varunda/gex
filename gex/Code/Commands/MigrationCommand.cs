using gex.Commands;
using gex.Models.Bar;
using gex.Services.Migrations;
using gex.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class MigrationCommand {

        private readonly ILogger<MigrationCommand> _Logger;
        private readonly BarMatchPlayerStartSpotMigration _PlayerStartSpotMigration;
        private readonly BarMapRepository _MapRepository;
        private readonly StartSpotDataMigration _StartSpotDataMigration;

        public MigrationCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<MigrationCommand>>();
            _PlayerStartSpotMigration = services.GetRequiredService<BarMatchPlayerStartSpotMigration>();
            _MapRepository = services.GetRequiredService<BarMapRepository>();
            _StartSpotDataMigration = services.GetRequiredService<StartSpotDataMigration>();
        }

        public Task PlayerStartSpotFixAll() {
            _Logger.LogInformation($"starting task to fix all player start spots");

            new Task(async () => {
                try {
                    using CancellationTokenSource cts = new(TimeSpan.FromHours(8));

                    await _PlayerStartSpotMigration.FixAll(cts.Token);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to run player start spot migration");
                }
            }).Start();

            return Task.CompletedTask;
        }

        public async Task PlayerStartSpotFixMap(string mapFilename) {
            BarMap? map = await _MapRepository.GetByFileName(mapFilename, CancellationToken.None);
            if (map == null) {
                _Logger.LogWarning($"failed to find map to fix player start spots for [map={mapFilename}]");
                return;
            }

            _Logger.LogInformation($"starting task to fix all player start spots on map [map={mapFilename}]");

            new Task(async () => {
                using CancellationTokenSource cts = new(TimeSpan.FromHours(8));

                await _PlayerStartSpotMigration.FixMap(map, cts.Token);
            }).Start();
        }

        public Task StartSpotData() {
            _Logger.LogInformation($"starting task to fix start spot data");

            new Task(async () => {
                try {
                    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(15));
                    await _StartSpotDataMigration.FixAll(cts.Token);
                    _Logger.LogInformation($"ran start spot data migration");
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to run start spot data migration");
                }
            }).Start();

            return Task.CompletedTask;
        }

    }
}
