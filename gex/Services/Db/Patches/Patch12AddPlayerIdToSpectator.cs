using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch12AddPlayerIdToSpectator : IDbPatch {
        public int MinVersion => 12;

        public string Name => "add player_id to bar_match_spectator";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_spectator
                    ADD COLUMN IF NOT EXISTS player_id int NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
