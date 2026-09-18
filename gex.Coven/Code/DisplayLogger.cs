using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using gex.Coven.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code {

    public class DisplayLogger : ILogger {

        private static string _UsernameToRedact = Environment.UserName;

        private string _Name { get; set; } = "";
        private Func<DisplayLoggerConfiguration> _GetCurrentConfig;

        public DisplayLogger(string name, Func<DisplayLoggerConfiguration> getCurrentConfig) {
            _Name = name;
            _GetCurrentConfig = getCurrentConfig;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
            return default!;
        }

        public bool IsEnabled(LogLevel logLevel) {
            return logLevel >= _GetCurrentConfig().LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (IsEnabled(logLevel) == false) {
                return;
            }

			char ps = Path.DirectorySeparatorChar;
            string message = formatter(state, exception)
				.Replace($"{ps}{_UsernameToRedact}{ps}", $"{ps}<username>{ps}", StringComparison.OrdinalIgnoreCase);

            WeakReferenceMessenger.Default.Send(new DisplayLoggerMessage() {
                Timestamp = DateTime.UtcNow,
                Level = LEVEL_NAMES[logLevel],
                Category = _Name,
                Message = message,
                BackgroundColor = LEVEL_BACKGROUND_COLORS[logLevel],
                Foreground = LEVEL_FORGROUND_COLORS[logLevel]
            });
        }

        private static readonly Dictionary<LogLevel, string> LEVEL_NAMES = new() {
            [LogLevel.Information] = "INFO",
            [LogLevel.Warning] = "WARN",
            [LogLevel.Error] = "FAIL",
            [LogLevel.Debug] = "DBUG",
            [LogLevel.Trace] = "TRCE",
        };

        private static readonly Dictionary<LogLevel, IBrush> LEVEL_BACKGROUND_COLORS = new() {
            [ LogLevel.Information ] = Brushes.Green,
            [ LogLevel.Warning ] = Brushes.Yellow,
            [ LogLevel.Error ] = Brushes.Red,
            [ LogLevel.Debug ] = Brushes.Blue,
            [ LogLevel.Trace ] = Brushes.Purple,
        };

        private static readonly Dictionary<LogLevel, IBrush> LEVEL_FORGROUND_COLORS = new() {
            [ LogLevel.Information ] = Brushes.White,
            [ LogLevel.Warning ] = Brushes.Black,
            [ LogLevel.Error ] = Brushes.Black,
            [ LogLevel.Debug ] = Brushes.White,
            [ LogLevel.Trace ] = Brushes.White,
        };

        public sealed class DisplayLoggerConfiguration {

            public LogLevel LogLevel { get; set; } = LogLevel.Information;

        }

    }


}
