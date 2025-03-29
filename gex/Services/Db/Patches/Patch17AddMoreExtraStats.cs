using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch17AddMoreExtraStats : IDbPatch {
		public int MinVersion => 17;
		public string Name => "add more to game_event_extra_stats";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);

			using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS total_value double precision NOT NULL default 0;

                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS defense_value double precision NOT NULL default 0;

                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS eco_value double precision NOT NULL default 0;

                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS util_value double precision NOT NULL default 0;

                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS other_value double precision NOT NULL default 0;
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
