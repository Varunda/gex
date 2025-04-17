using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch18AddGameVersionUsage : IDbPatch {
		public int MinVersion => 18;
		public string Name => "add game_version_usage";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS game_version_usage (
					engine varchar NOT NULL,
					version varchar NOT NULL,
					last_used timestamptz NOT NULL,
					deleted_on timestamptz NULL,

					PRIMARY KEY (engine, version)
				);

                INSERT INTO game_version_usage (
                    engine, version, last_used
                ) SELECT engine, game_version, max(start_time)
                    FROM bar_match
                    GROUP BY engine, game_version
                ON CONFLICT (engine, version) DO NOTHING;
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
