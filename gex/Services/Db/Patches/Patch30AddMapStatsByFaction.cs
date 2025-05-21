using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch30AddMapStatsByFaction : IDbPatch {
		public int MinVersion => 30;
		public string Name => "add map_stats_by_faction";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS map_stats_by_faction (
					map_file_name varchar NOT NULL,
					gamemode smallint NOT NULL,
					faction smallint NOT NULL,

					timestamp timestamptz NOT NULL,

					play_count_all_time int NOT NULL,
					win_count_all_time int NOT NULL,

					play_count_month int NOT NULL,
					win_count_month int NOT NULL,

					play_count_week int NOT NULL,
					win_count_week int NOT NULL,

					play_count_day int NOT NULL,
					win_count_day int NOT NULL,

					PRIMARY KEY (map_file_name, gamemode, faction)
				);
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
