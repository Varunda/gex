using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Util {

    public class TestLoggerFactory : ILoggerFactory {

        public void AddProvider(ILoggerProvider provider) {
        }

        public ILogger CreateLogger(string categoryName) {
            return new TestLogger(categoryName, true);
        }

        public void Dispose() { }

    }
}
