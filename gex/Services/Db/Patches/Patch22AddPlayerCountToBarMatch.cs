using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch22AddPlayerCountToBarMatch : IDbPatch {
        public int MinVersion => 22;
        public string Name => "add player_count to bar_match";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
				ALTER TABLE bar_match
					ADD COLUMN IF NOT EXISTS player_count int NOT NULL DEFAULT 0;
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
