using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

	/// <summary>
	///		keeps track of when a game version is last used
	/// </summary>
	[DapperColumnsMapped]
	public class GameVersionUsage {

		/// <summary>
		///		engine this version is stored in
		/// </summary>
		[ColumnMapping("engine")]
		public string Engine { get; set; } = "";

		/// <summary>
		///		game version
		/// </summary>
		[ColumnMapping("version")]
		public string Version { get; set; } = "";

		/// <summary>
		///		when this game version was last used
		/// </summary>
		[ColumnMapping("last_used")]
		public DateTime LastUsed { get; set; }

		/// <summary>
		///		when this game version was deleted from local disk
		/// </summary>
		[ColumnMapping("deleted_on")]
		public DateTime? DeletedOn { get; set; }

	}
}
