using gex.Models.Db;
using gex.Models.Event;
using gex.Services.Db.Event;
using gex.Services.Repositories;
using gex.Services.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.BackgroundTasks {

    public class UnitPositionCompressionService : IHostedService {

        private readonly ILogger<UnitPositionCompressionService> _Logger;
        private readonly UnitPositionFileStorage _UnitPositionStorage;
        private readonly BarMatchRepository _MatchRepository;
        private readonly GameEventUnitPositionDb _UnitPositionDb;
        private readonly BarMatchProcessingRepository _ProcessingRepository;

        public UnitPositionCompressionService(ILogger<UnitPositionCompressionService> logger,
            UnitPositionFileStorage unitPositionStorage, BarMatchRepository matchRepository,
            GameEventUnitPositionDb unitPositionDb, BarMatchProcessingRepository processingRepository) {

            _Logger = logger;
            _UnitPositionStorage = unitPositionStorage;
            _MatchRepository = matchRepository;
            _UnitPositionDb = unitPositionDb;
            _ProcessingRepository = processingRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            Task task = Task.Run(async () => {
                Stopwatch allTimer = Stopwatch.StartNew();

                _Logger.LogInformation($"starting unit position compresssion");
                List<BarMatchProcessing> procs = await _ProcessingRepository.NeedsUnitPositionCompression(cancellationToken);

                _Logger.LogInformation($"compressing unit position data [count={procs.Count}]");

                foreach (BarMatchProcessing proc in procs) {
                    if (_UnitPositionStorage.IsSaved(proc.GameID) == false) {
                        Stopwatch timer = Stopwatch.StartNew();

                        List<GameEventUnitPosition> pos = await _UnitPositionDb.GetByGameID(proc.GameID, cancellationToken);
                        long loadMs = timer.ElapsedMilliseconds; timer.Restart();

                        try {
                            await _UnitPositionStorage.SaveToDisk(proc.GameID, pos, cancellationToken);
                        } catch (Exception ex) {
                            _Logger.LogError(ex, $"failed to compress unit position data [gameID={proc.GameID}]");
                        }

                        long saveMs = timer.ElapsedMilliseconds; 

                        _Logger.LogDebug($"compressing unit position data for match [gameID={proc.GameID}] [count={pos.Count}] "
                            + $"[timer={loadMs + saveMs}ms] [load={loadMs}ms] [save={saveMs}ms]");
                    }

                    BarMatchProcessing processing = await _ProcessingRepository.GetByGameID(proc.GameID, cancellationToken)
                        ?? throw new Exception($"actually what");

                    processing.UnitPositionCompressed = true;
                    await _ProcessingRepository.Upsert(processing);
                }

                _Logger.LogInformation($"finished compresssing unit data [timer={allTimer.ElapsedMilliseconds}ms] [count={procs.Count}]");
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
