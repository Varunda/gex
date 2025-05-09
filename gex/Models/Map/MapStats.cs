using System;

namespace gex.Models.Map {

	public class MapStats {

		public string MapFileName { get; set; } = "";

		public DateTime Timestamp { get; set; }

		public int PlayCount24h { get; set; }

		public int PlayCount1w { get; set; }

		public int PlayCount1m { get; set; }

		public int PlayCountAllTime { get; set; }

		public double AverageDurationMs { get; set; }

		public double MedianDurationMs { get; set; }

	}
}
