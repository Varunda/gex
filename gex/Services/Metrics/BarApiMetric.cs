using gex.Code;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace gex.Services.Metrics {

	[MetricName(BarApiMetric.NAME)]
	public class BarApiMetric {

		public const string NAME = "Gex.BarApi";

		private readonly Meter _Meter;

		private readonly Counter<long> _Uses;
		private readonly Histogram<double> _Duration;

		public BarApiMetric(IMeterFactory factory) {
			_Meter = factory.Create(NAME);

			_Uses = _Meter.CreateCounter<long>(
				name: "gex_bar_api_use",
				description: "endpoints hit from the BAR API"
			);

			_Duration = _Meter.CreateHistogram<double>(
				name: "gex_bar_api_duration",
				description: "how long each endpoint is taking to hit"
			);
		}

		public void RecordUse(string type) {
			_Uses.Add(1,
				new KeyValuePair<string, object?>("type", type)
			);
		}

		public void RecordDuration(string type, double durationSec) {
			_Duration.Record(durationSec,
				new KeyValuePair<string, object?>("type", type)
			);
		}

	}
}
