using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Util {

    public class TestMeterFactory : IMeterFactory {

        public Meter Create(MeterOptions options) {
            return new Meter(options);
        }

        public void Dispose() {
        }

    }
}
