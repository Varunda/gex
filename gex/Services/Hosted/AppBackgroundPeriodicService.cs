using gex.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted {

    public abstract class AppBackgroundPeriodicService : BackgroundService {

        private readonly string _ServiceName;
        private readonly TimeSpan _RunDelay;

        protected readonly ILogger _Logger;
        private readonly ServiceHealthMonitor _ServiceHealthMonitor;

        public AppBackgroundPeriodicService(string serviceName, TimeSpan runDelay,
            ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon) {

            _ServiceName = serviceName;
            _RunDelay = runDelay;

            _Logger = loggerFactory.CreateLogger($"gex.Services.Hosted.{_ServiceName}");
            _ServiceHealthMonitor = healthMon;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"starting background service [name={_ServiceName}]");

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    Stopwatch timer = Stopwatch.StartNew();

                    ServiceHealthEntry entry = _ServiceHealthMonitor.Get(_ServiceName)
                        ?? new ServiceHealthEntry() { Name = _ServiceName };

                    if (entry.Enabled == false) {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    try {
                        string? output = await PerformTask(stoppingToken);
                        entry.Message = output ?? "";
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"error performing background periodic task");
                        entry.Message = ex.Message;
                    }

                    long elapsedTime = timer.ElapsedMilliseconds;

                    entry.RunDuration = elapsedTime;
                    entry.LastRan = DateTime.UtcNow;
                    _ServiceHealthMonitor.Set(_ServiceName, entry);

                    long timeToHold = (long)_RunDelay.TotalMilliseconds - elapsedTime;

                    if (timeToHold > 5) {
                        await Task.Delay((int)timeToHold, stoppingToken);
                    }
                } catch (Exception) when (stoppingToken.IsCancellationRequested == true) {
                    _Logger.LogInformation($"Stopped data builder service");
                } catch (Exception ex) {
                    _Logger.LogError(ex, "Exception in DataBuilderService");
                }
            }
        }

        /// <summary>
        ///     perform the task, optionally giving a message back that indicates the status of the ran task
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        protected abstract Task<string?> PerformTask(CancellationToken cancel);

    }
}
