using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch09AddGameEventTeamDied : IDbPatch {
        public int MinVersion => 9;
        public string Name => "add game_event_team_died";

        public async Task Execute(IDbHelper helper) {

            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS game_event_team_died (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    team_id int NOT NULL,

                    CONSTRAINT unq_team_died_game_id_team_id UNIQUE (game_id, team_id)
                );

                CREATE INDEX IF NOT EXISTS idx_team_died_game_id ON game_event_team_died (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
