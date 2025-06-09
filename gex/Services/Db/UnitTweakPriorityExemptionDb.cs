using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

	public class UnitTweakPriorityExemptionDb {

		private readonly ILogger<UnitTweakPriorityExemptionDb> _Logger;
		private readonly IDbHelper _DbHelper;

		public UnitTweakPriorityExemptionDb(ILogger<UnitTweakPriorityExemptionDb> logger,
			IDbHelper dbHelper) {

			_Logger = logger;
			_DbHelper = dbHelper;
		}

		public async Task<List<UnitTweakPriorityExemption>> GetAll(CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			return await conn.QueryListAsync<UnitTweakPriorityExemption>(@"
				SELECT * FROM unit_tweak_exemption;
			", cancel);
		}

	}
}
