using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch10AddGameEventUnitResources : IDbPatch {

        public int MinVersion => 10;
        public string Name => "add game_event_unit_resources";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS game_event_unit_resources (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    metal_made double precision NOT NULL,
                    metal_used double precision NOT NULL,
                    energy_made double precision NOT NULL,
                    energy_used double precision NOT NULL,

                    CONSTRAINT unq_event_unit_resources_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_unit_resources ON game_event_unit_resources (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
