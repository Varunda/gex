using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch65AddBarMatchDrawMapPoint : IDbPatch {
        public int MinVersion => 65;
        public string Name => "add bar_match_map_draw_point";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_match_map_draw_point (
                    game_id varchar NOT NULL,
                    player_id smallint NOT NULL,
                    game_time float NOT NULL,
                    index int NOT NULL,
                    x float NOT NULL,
                    z float NOT NULL,
                    label varchar NOT NULL,
                    from_lua smallint NOT NULL,

                    CONSTRAINT unq_bar_match_map_draw_point UNIQUE (game_id, player_id, index, game_time, x, z)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_match_map_draw_point_game_id ON bar_match_map_draw_point (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
