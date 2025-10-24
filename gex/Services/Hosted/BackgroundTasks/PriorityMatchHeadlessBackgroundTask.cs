using gex.Common.Models;
using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.BackgroundTasks {

    public class PriorityMatchHeadlessBackgroundTask : BackgroundService {

        private const string SERVICE_NAME = "priority_headless_runner";

        private const int MAX_INSTANCES = 2;

        private static int _InstanceId = 0;

        private DateTime? _LastInformedOfDisabled = null;

        private readonly ILogger<PriorityMatchHeadlessBackgroundTask> _Logger;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BarHeadlessInstance _HeadlessRunner;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogParseQueue;
        private readonly ServiceHealthMonitor _ServiceHealthMonitor;

        public PriorityMatchHeadlessBackgroundTask(ILogger<PriorityMatchHeadlessBackgroundTask> logger,
            BarMatchProcessingRepository processingRepository, BarHeadlessInstance headlessRunner,
            BaseQueue<ActionLogParseQueueEntry> actionLogParseQueue, ServiceHealthMonitor serviceHealthMonitor) {

            _Logger = logger;
            _ProcessingRepository = processingRepository;
            _HeadlessRunner = headlessRunner;
            _ActionLogParseQueue = actionLogParseQueue;
            _ServiceHealthMonitor = serviceHealthMonitor;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Task.Run(async () => {
                ServiceHealthEntry healthEntry = _ServiceHealthMonitor.GetOrCreate(SERVICE_NAME);
                _ServiceHealthMonitor.Set(SERVICE_NAME, healthEntry);

                try {
                    await Run(stoppingToken);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"error in background priority runner");
                }
            }, stoppingToken);
        }

        private async Task Run(CancellationToken cancel) {

            int instanceId = _InstanceId++;

            _Logger.LogDebug($"starting instance of service [service={SERVICE_NAME}] [instanceId={instanceId}]");

            int errorsInARow = 0;

            while (cancel.IsCancellationRequested == false) {
                ServiceHealthEntry healthEntry = _ServiceHealthMonitor.GetOrCreate(SERVICE_NAME);
                if (healthEntry.Enabled == false) {
                    if (_LastInformedOfDisabled == null || (DateTime.UtcNow - _LastInformedOfDisabled) > TimeSpan.FromMinutes(5)) {
                        _Logger.LogInformation($"reminder: service {SERVICE_NAME} is disabled (5m cooldown)");
                        _LastInformedOfDisabled = DateTime.UtcNow;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), cancel);
                    continue;
                }

                try {
                    if ((instanceId + 1) <= healthEntry.MaxCount) {
                        if (_LastInformedOfDisabled == null || (DateTime.UtcNow - _LastInformedOfDisabled) > TimeSpan.FromMinutes(5)) {
                            _Logger.LogInformation($"reminder: service {SERVICE_NAME} instance {instanceId} is disabled (5m cooldown)");
                            _LastInformedOfDisabled = DateTime.UtcNow;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5), cancel);
                        continue;
                    }

                    _ServiceHealthMonitor.Set(SERVICE_NAME, healthEntry);

                    _Logger.LogDebug($"looking for a game to process based on priority");

                    Stopwatch timer = Stopwatch.StartNew();

                    BarMatchProcessing? nextPrio = await _ProcessingRepository.GetLowestPriority(cancel);
                    if (nextPrio == null) {
                        _Logger.LogDebug($"no games to run based on priority, waiting a while and checking again");
                        await Task.Delay(TimeSpan.FromSeconds(15), cancel);
                        continue;
                    }

                    _Logger.LogDebug($"found game to process based on priority [gameID={nextPrio.GameID}] [priority={nextPrio.Priority}]");

                    Result<GameOutput, string> output = await _HeadlessRunner.RunGame(nextPrio.GameID, false, cancel);
                    if (output.IsOk == false) {
                        _Logger.LogError($"failed to process game in headless mode (priority) [gameID={nextPrio.GameID}] [error={output.Error}]");
                        continue;
                    }

                    BarMatchProcessing processing = await _ProcessingRepository.GetByGameID(nextPrio.GameID, cancel)
                        ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {nextPrio.GameID}");

                    processing.ReplaySimulated = DateTime.UtcNow;
                    processing.ReplaySimulatedMs = (int)timer.ElapsedMilliseconds;
                    await _ProcessingRepository.Upsert(processing);

                    _Logger.LogDebug($"putting entry into action log parser [gameID={nextPrio.GameID}]");
                    _ActionLogParseQueue.Queue(new ActionLogParseQueueEntry() {
                        GameID = nextPrio.GameID,
                    });

                    errorsInARow = 0;
                    healthEntry.RunDuration = timer.ElapsedMilliseconds;
                    healthEntry.Message = $"locally ran game {nextPrio.GameID}";

                } catch (Exception ex) when (cancel.IsCancellationRequested == false) {
                    ++errorsInARow;
                    _Logger.LogError(ex, $"failed to run priority process");

                    if (errorsInARow >= 50) {
                        _Logger.LogError($"got more than 50 errors in a row, pausing background service for 5m");
                        await Task.Delay(TimeSpan.FromMinutes(5), cancel);
                        errorsInARow = 0;
                    }
                } catch (Exception) when (cancel.IsCancellationRequested == true) {
                    _Logger.LogInformation($"stopping service");
                }

                healthEntry.LastRan = DateTime.UtcNow;
                _ServiceHealthMonitor.Set(SERVICE_NAME, healthEntry);
            }

        }

    }
}
