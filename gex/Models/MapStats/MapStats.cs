using System.Collections.Generic;

namespace gex.Models.MapStats {

	public class MapStats {

		public string MapFilename { get; set; } = "";

		public List<MapStatsByGamemode> Stats { get; set; } = [];

		public List<MapStatsStartSpot> StartSpots { get; set; } = [];

	}
}
