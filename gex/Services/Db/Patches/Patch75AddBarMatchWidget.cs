using gex.Common.Services.Db;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch75AddBarMatchWidget : IDbPatch {
        public int MinVersion => 75;
        public string Name => "add bar_match_widget";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_match_player_widget (
                    game_id varchar NOT NULL,
                    user_id bigint NOT NULL,
                    hash varchar NOT NULL,
                    state int NOT NULL,

                    PRIMARY KEY (game_id, user_id, hash)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_match_player_widget_game_id ON bar_match_player_widget(game_id);

                CREATE INDEX IF NOT EXISTS idx_bar_match_player_widget_user_id ON bar_match_player_widget(user_id);

                CREATE INDEX IF NOT EXISTS idx_bar_match_player_widget_hash ON bar_match_player_widget(hash);

                CREATE TABLE IF NOT EXISTS bar_widget_entry (
                    hash varchar NOT NULL PRIMARY KEY,
                    name varchar NOT NULL,
                    author varchar NOT NULL,
                    description varchar NOT NULL,
                    timestamp timestamptz NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
