using gex.Code.Constants;
using System;

namespace gex.Models.Map {

	public class MapStatsByGamemode {

		public string MapFileName { get; set; } = "";

		public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

		public DateTime Timestamp { get; set; }

		public int PlayCountDay { get; set; }

		public int PlayCountWeek { get; set; }

		public int PlayCountMonth { get; set; }

		public int PlayCountAllTime { get; set; }

		public double AverageDurationMs { get; set; }

		public double MedianDurationMs { get; set; }

	}

}
