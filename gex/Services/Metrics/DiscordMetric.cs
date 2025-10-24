using gex.Common.Code;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace gex.Services.Metrics {

    [MetricName(DiscordMetric.NAME)]
    public class DiscordMetric {

        public const string NAME = "Gex.Discord";

        private readonly Meter _Meter;

        private readonly Counter<long> _Uses;
        private readonly Histogram<double> _Duration;

        public DiscordMetric(IMeterFactory factory) {
            _Meter = factory.Create(NAME);

            _Uses = _Meter.CreateCounter<long>(
                name: "gex_discord_command_use",
                description: "counter for the different commands within gex being used"
            );
            _Duration = _Meter.CreateHistogram<double>(
                name: "gex_discord_command_duration",
                description: "how long for different command within gex"
            );
        }

        public void RecordUse(string type, string interaction) {
            _Uses.Add(1,
                new KeyValuePair<string, object?>("type", type),
                new KeyValuePair<string, object?>("interaction", interaction)
            );
        }

        public void RecordDuration(string type, double durationSec) {
            _Duration.Record(durationSec,
                new KeyValuePair<string, object?>("type", type)
            );
        }

    }
}
