using gex.Code.Constants;
using System;

namespace gex.Models.Db {

	/// <summary>
	///		search parameters when searching the database. If an option is null, it is not used
	/// </summary>
	public class BarMatchSearchParameters {

		/// <summary>
		///		exact match to <see cref="BarMatch.Engine"/>
		/// </summary>
		public string? EngineVersion { get; set; }

		/// <summary>
		///		exact match to <see cref="BarMatch.GameVersion"/>
		/// </summary>
		public string? GameVersion { get; set; }

		/// <summary>
		///		exact match to <see cref="BarMatch.Map"/>
		/// </summary>
		public string? Map { get; set; }

		/// <summary>
		///		the value of <see cref="BarMatch.StartTime"/> must come AFTER this value
		/// </summary>
		public DateTime? StartTimeAfter { get; set; }

		/// <summary>
		///		the value of <see cref="BarMatch.StartTime"/> must come BEFORE this value
		/// </summary>
		public DateTime? StartTimeBefore { get; set; }

		/// <summary>
		///		the value of <see cref="BarMatch.DurationMs"/> must be greater than this value
		/// </summary>
		public long? DurationMinimum { get; set; }

		/// <summary>
		///		the value of <see cref="BarMatch.DurationMs"/> must be equal or less than this value
		/// </summary>
		public long? DurationMaximum { get; set; }

		/// <summary>
		///		does the game have to be ranked or not?
		/// </summary>
		public bool? Ranked { get; set; }

		/// <summary>
		///		gamemode, see <see cref="BarGamemode"/> for which values are what
		/// </summary>
		public byte? Gamemode { get; set; }

		/// <summary>
		///		has this match been downloaded
		/// </summary>
		public bool? ProcessingDownloaded { get; set; }

		/// <summary>
		///		has the demofile of this been parsed
		/// </summary>
		public bool? ProcessingParsed { get; set; }

		/// <summary>
		///		has this match been replayed locally?
		/// </summary>
		public bool? ProcessingReplayed { get; set; }

		/// <summary>
		///		has the action log of this match from a local replay been parsed?
		/// </summary>
		public bool? ProcessingAction { get; set; }

		/// <summary>
		///		minimum number of players to be included
		/// </summary>
		public int? PlayerCountMinimum { get; set; }

		/// <summary>
		///		minimum number of players to be included
		/// </summary>
		public int? PlayerCountMaximum { get; set; }

		/// <summary>
		///		is legion enabled or disdabled
		/// </summary>
		public bool? LegionEnabled { get; set; }

	}
}
