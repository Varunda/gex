using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch27AddMapStats : IDbPatch {
		public int MinVersion => 27;
		public string Name => "add map_stats";

		public async Task Execute(IDbHelper helper) {

			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS map_stats (
					map_file_name varchar NOT NULL,
					gamemode smallint NOT NULL,

					timestamp timestamptz NOT NULL,
					play_count_all_time int NOT NULL,
					play_count_week int NOT NULL,
					play_count_month int NOT NULL,
					play_count_day int NOT NULL,
				
					duration_average_ms double precision NOT NULL,
					duration_median_ms double precision NOT NULL,

					PRIMARY KEY (map_file_name, gamemode)
				);
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
