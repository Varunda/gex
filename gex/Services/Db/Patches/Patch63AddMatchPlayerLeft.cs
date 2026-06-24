using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch63AddMatchPlayerLeft : IDbPatch {
        public int MinVersion => 63;
        public string Name => "add bar_match_player_left";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_match_player_left (
                    game_id varchar NOT NULL,
                    player_id smallint NOT NULL,
                    reason smallint NOT NULL,
                    game_time float NOT NULL,
                    index int NOT NULL,

                    CONSTRAINT unq_bar_match_player_left UNIQUE (game_id, player_id, game_time, index)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_match_player_left_game_id ON bar_match_player_left (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
