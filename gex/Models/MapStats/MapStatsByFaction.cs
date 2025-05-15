using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.Constants;
using System;

namespace gex.Models.MapStats {

	[DapperColumnsMapped]
	public class MapStatsByFaction {

		[ColumnMapping("map_file_name")]
		public string MapFileName { get; set; } = "";

		[ColumnMapping("gamemode")]
		public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

		[ColumnMapping("faction")]
		public byte Faction { get; set; } = BarFaction.DEFAULT;

		[ColumnMapping("timestamp")]
		public DateTime Timestamp { get; set; }

		[ColumnMapping("play_count_all_time")]
		public int PlayCountAllTime { get; set; }

		[ColumnMapping("win_count_all_time")]
		public int WinCountAllTime { get; set; }

		[ColumnMapping("play_count_month")]
		public int PlayCountMonth { get; set; }

		[ColumnMapping("win_count_month")]
		public int WinCountMonth { get; set; }

		[ColumnMapping("play_count_week")]
		public int PlayCountWeek { get; set; }

		[ColumnMapping("win_count_week")]
		public int WinCountWeek { get; set; }

		[ColumnMapping("play_count_day")]
		public int PlayCountDay { get; set; }

		[ColumnMapping("win_count_day")]
		public int WinCountDay { get; set; }

	}
}
