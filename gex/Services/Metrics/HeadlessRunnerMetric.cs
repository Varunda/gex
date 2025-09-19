using gex.Code;
using System.Diagnostics.Metrics;

namespace gex.Services.Metrics {

    [MetricName(BarApiMetric.NAME)]
    public class HeadlessRunnerMetric {

        public const string NAME = "Gex.Headless";

        private readonly Meter _Meter;

        private readonly Histogram<double> _Startup;

        public HeadlessRunnerMetric(IMeterFactory factory) {
            _Meter = factory.Create(NAME);

            _Startup = _Meter.CreateHistogram<double>(
                name: "gex_headless_startup",
                description: "how long it takes a headless instance to startup"
            );
        }

        public void RecordStartup(long startupMs) {
            _Startup.Record(startupMs / 1000d);
        }

    }
}
