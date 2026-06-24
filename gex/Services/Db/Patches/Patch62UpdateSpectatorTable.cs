using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch62UpdateSpectatorTable : IDbPatch {
        public int MinVersion => 62;
        public string Name => "update bar_match_spectator with user_id_can_be_wrong and nullable user_id";

        public async Task Execute(IDbHelper helper) {

            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_spectator
                    ADD COLUMN IF NOT EXISTS user_id_can_be_wrong bool DEFAULT false;

                ALTER TABLE bar_match_spectator
                    DROP CONSTRAINT IF EXISTS unq_spectator_game_id_and_user_id;

                ALTER TABLE bar_match_spectator
                    ALTER COLUMN user_id DROP NOT NULL;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
