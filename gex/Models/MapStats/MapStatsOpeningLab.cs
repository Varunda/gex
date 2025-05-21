using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Constants;
using System;

namespace gex.Models.MapStats {

	[DapperColumnsMapped]
	public class MapStatsOpeningLab {

		[ColumnMapping("map_file_name")]
		public string MapFilename { get; set; } = "";

		[ColumnMapping("map_file_name")]
		public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

		[ColumnMapping("def_name")]
		public string DefName { get; set; } = "";

		[ColumnMapping("timestamp")]
		public DateTime Timestamp { get; set; }

		[ColumnMapping("count_total")]
		public int CountTotal { get; set; }

		[ColumnMapping("count_win")]
		public int CountWin { get; set; }

	}
}
