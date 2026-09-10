using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code {

    [ProviderAlias("DisplayLogger")]
    [UnsupportedOSPlatform("browser")]
    public class DisplayLoggerProvider : ILoggerProvider {

        private readonly IDisposable? _OnChangeToken;
        private DisplayLogger.DisplayLoggerConfiguration _Config;
        private readonly ConcurrentDictionary<string, DisplayLogger> _Loggers = new(StringComparer.OrdinalIgnoreCase);

        public DisplayLoggerProvider(IOptionsMonitor<DisplayLogger.DisplayLoggerConfiguration> config) {
            _Config = config.CurrentValue;
            _OnChangeToken = config.OnChange(updated => {
                _Config = updated;
            });
        }

        public ILogger CreateLogger(string name) {
            return _Loggers.GetOrAdd(name, (newName) => {
                return new DisplayLogger(newName, () => _Config);
            });
        }

        public void Dispose() {
            _Loggers.Clear();
            _OnChangeToken?.Dispose();
        }

    }
}
