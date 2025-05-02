using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch23AddProcessingPriority : IDbPatch {
		public int MinVersion => 23;
		public string Name => "add priority to bar_match_processing";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_processing ADD COLUMN IF NOT EXISTS priority smallint NOT NULL DEFAULT 100;
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
