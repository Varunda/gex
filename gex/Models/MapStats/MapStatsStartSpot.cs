using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.MapStats {

	[DapperColumnsMapped]
	public class MapStatsStartSpot {

		[ColumnMapping("map_file_name")]
		public string MapFilename { get; set; } = "";

		[ColumnMapping("gamemode")]
		public byte Gamemode { get; set; }

		[ColumnMapping("timestamp")]
		public DateTime Timestamp { get; set; }

		[ColumnMapping("start_x")]
		public int StartX { get; set; }

		[ColumnMapping("start_z")]
		public int StartZ { get; set; }

		[ColumnMapping("count_total")]
		public int CountTotal { get; set; }

		[ColumnMapping("count_win")]
		public int CountWin { get; set; }

	}
}
