using gex.Common.Code.ExtensionMethods;
using gex.Models;
using gex.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace gex.Commands {

    [Command]
    public class ServiceCommand {

        private readonly ILogger<ServiceCommand> _Logger;
        private readonly ServiceHealthMonitor _ServiceHealthMonitor;

        public ServiceCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<ServiceCommand>>();
            _ServiceHealthMonitor = services.GetRequiredService<ServiceHealthMonitor>();
        }

        public void Print() {
            List<string> services = _ServiceHealthMonitor.GetServices();

            string s = $"Services running: {services.Count}\n";
            s += $"{"service",-24} | {"last ran",30} | {"time ago",10} | {"run time",12} | {"message",160}\n";

            DateTimeOffset now = DateTimeOffset.UtcNow;

            foreach (string service in services) {
                ServiceHealthEntry? entry = _ServiceHealthMonitor.Get(service);

                if (entry != null) {
                    TimeSpan timeAgo = now - entry.LastRan;
                    string ago = $"{timeAgo.Minutes:D2}:{timeAgo.Seconds:D2}";

                    s += $"{entry.Name,-40} | {entry.LastRan,30} | {ago,10} | {entry.RunDuration,10}ms | {entry.Message?.Truncate(160)}\n";
                }
            }

            _Logger.LogInformation(s);
        }

        public void Enable(string serviceName) {
            ServiceHealthEntry? entry = _ServiceHealthMonitor.Get(serviceName);
            if (entry == null) {
                _Logger.LogWarning($"cannot enable service '{serviceName}': does not exist");
            } else {
                entry.Enabled = true;
                _ServiceHealthMonitor.Set(serviceName, entry);
                _Logger.LogInformation($"enabled service '{serviceName}'");
            }
        }

        public void Disable(string serviceName) {
            ServiceHealthEntry entry = _ServiceHealthMonitor.GetOrCreate(serviceName);
            entry.Enabled = false;
            _ServiceHealthMonitor.Set(serviceName, entry);

            _Logger.LogInformation($"disabled service '{serviceName}'");
        }

        public void Count(string serviceName, int count) {
            ServiceHealthEntry entry = _ServiceHealthMonitor.GetOrCreate(serviceName);
            entry.MaxCount = count;
            _ServiceHealthMonitor.Set(serviceName, entry);

            _Logger.LogInformation($"set instance count of service '{serviceName}' to {count}");
        }

    }

}