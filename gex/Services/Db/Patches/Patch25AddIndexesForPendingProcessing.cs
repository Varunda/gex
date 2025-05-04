using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch25AddIndexesForPendingProcessing : IDbPatch {
		public int MinVersion => 25;
		public string Name => "add index for pending processing";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE INDEX IF NOT EXISTS idx_bar_match_player_count ON bar_match (player_count);
				CREATE INDEX IF NOT EXISTS idx_match_processing_demofile_fetched ON bar_match_processing (demofile_fetched);
				CREATE INDEX IF NOT EXISTS idx_match_processing_demofile_parsed ON bar_match_processing (demofile_parsed);
				CREATE INDEX IF NOT EXISTS idx_match_processing_headless_ran ON bar_match_processing (headless_ran);
				CREATE INDEX IF NOT EXISTS idx_match_processing_actions_parsed ON bar_match_processing (actions_parsed);
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
