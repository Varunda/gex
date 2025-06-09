using Microsoft.Extensions.Logging;
using System;

namespace gex.Tests.Util {

    public class TestLogger<T> : ILogger<T> {

        private string _ClassPrefix;

        public bool OutputEnabled { get; set; } = true;

        public TestLogger() {
            _ClassPrefix = typeof(T).FullName ?? typeof(T).Name;
        }

        public TestLogger(bool outputEnabled) {
            _ClassPrefix = typeof(T).FullName ?? typeof(T).Name;
            OutputEnabled = outputEnabled;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel) {
            return OutputEnabled;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (IsEnabled(logLevel) == false) {
                return;
            }

            Console.WriteLine($"[{DateTime.UtcNow:u}] {logLevel}: {_ClassPrefix}> {formatter(state, exception)}");
        }

    }

}
