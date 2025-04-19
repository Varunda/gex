using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch21AddGameEventUnitPosition : IDbPatch {
		public int MinVersion => 21;
		public string Name => "add game_event_unit_position";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS game_event_unit_position (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
					x double precision NOT NULL,
					y double precision NOT NULL,
					z double precision NOT NULL,

                    CONSTRAINT unq_unit_position_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
				);

				CREATE INDEX IF NOT EXISTS idx_event_unit_position ON game_event_unit_position (game_id);
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
