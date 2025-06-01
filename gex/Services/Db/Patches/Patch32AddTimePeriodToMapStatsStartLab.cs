using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch32AddTimePeriodToMapStatsStartLab : IDbPatch {
		public int MinVersion => 32;
		public string Name => "add time periods to map_stats_opening_lab";

		public async Task Execute(IDbHelper helper) {
            if ((await helper.HasColumn("map_stats_opening_lab", "count_win")) == true) {
                using NpgsqlConnection conn2 = helper.Connection(Dbs.MAIN);
                using NpgsqlCommand cmd2 = await helper.Command(conn2, @"
                    ALTER TABLE map_stats_opening_lab RENAME COLUMN count_win TO win_total;
                ");

                await cmd2.ExecuteNonQueryAsync();
                await conn2.CloseAsync();
            }

			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				ALTER TABLE map_stats_opening_lab
					ADD COLUMN IF NOT EXISTS count_month int NOT NULL DEFAULT 0;
				ALTER TABLE map_stats_opening_lab
					ADD COLUMN IF NOT EXISTS win_month int NOT NULL DEFAULT 0;

				ALTER TABLE map_stats_opening_lab
					ADD COLUMN IF NOT EXISTS count_week int NOT NULL DEFAULT 0;
				ALTER TABLE map_stats_opening_lab
					ADD COLUMN IF NOT EXISTS win_week int NOT NULL DEFAULT 0;

				ALTER TABLE map_stats_opening_lab
					ADD COLUMN IF NOT EXISTS count_day int NOT NULL DEFAULT 0;
				ALTER TABLE map_stats_opening_lab
					ADD COLUMN IF NOT EXISTS win_day int NOT NULL DEFAULT 0;
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
