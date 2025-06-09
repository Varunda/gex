using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

	[DapperColumnsMapped]
	public class UnitTweakPriorityExemption {

		[ColumnMapping("unit_tweak")]
		public string UnitTweak { get; set; } = "";

		[ColumnMapping("timestamp")]
		public DateTime Timestamp { get; set; }

	}
}
