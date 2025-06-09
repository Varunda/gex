using gex.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace gex.Services {

    public class ServiceHealthMonitor {

        private readonly ILogger<ServiceHealthMonitor> _Logger;

        private ConcurrentDictionary<string, ServiceHealthEntry> _Entries = new ConcurrentDictionary<string, ServiceHealthEntry>();

        public ServiceHealthMonitor(ILogger<ServiceHealthMonitor> logger) {
            _Logger = logger;
        }

        public ServiceHealthEntry? Get(string serviceName) {
            lock (_Entries) {
                _Entries.TryGetValue(serviceName, out ServiceHealthEntry? entry);
                return entry;
            }
        }

        public void Set(string serviceName, ServiceHealthEntry entry) {
            lock (_Entries) {
                _Entries.AddOrUpdate(serviceName, entry, (key, value) => entry);
            }
        }

        public List<string> GetServices() {
            lock (_Entries) {
                return new List<string>(_Entries.Keys);
            }
        }

    }

    public static class ServiceHealthMonitorExtensions {

        /// <summary>
        ///     Get the <see cref="ServiceHealthEntry"/> with <see cref="ServiceHealthEntry.Name"/> of <paramref name="serviceName"/>,
        ///     creating it if it does not exist
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static ServiceHealthEntry GetOrCreate(this ServiceHealthMonitor monitor, string serviceName) {
            return monitor.Get(serviceName) ?? new ServiceHealthEntry() {
                Name = serviceName,
                Enabled = true
            };
        }

    }

}