using gex.Code;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace gex.Services.Metrics {

    [MetricName(QueueMetric.NAME)]
    public class QueueMetric {

        public const string NAME = "Gex.Queue";

        private readonly Meter _Meter;

        private readonly Counter<long> _Count;
        private readonly Histogram<double> _Duration;

        public QueueMetric(IMeterFactory factory) {
            _Meter = factory.Create(NAME);

            _Count = _Meter.CreateCounter<long>(
                name: "gex_queue_count",
                description: "how many items have been inserted into the queue"
            );

            _Duration = _Meter.CreateHistogram<double>(
                name: "gex_queue_duration",
                description: "histogram of how long it takes to process an entry within a queue",
                unit: "s"
            );
        }

        /// <summary>
        ///		record an entry being added to a queue
        /// </summary>
        /// <param name="queue"></param>
        public void RecordCount(string queue) {
            _Count.Add(1,
                new KeyValuePair<string, object?>("queue", queue)
            );
        }

        /// <summary>
        ///		record the duration it took to process a queue entry
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="durationSec"></param>
        public void RecordDuration(string queue, double durationSec) {
            _Duration.Record(durationSec,
                new KeyValuePair<string, object?>("queue", queue)
            );
        }

    }
}
