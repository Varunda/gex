using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch52AddHiddenToMatchPool : IDbPatch {
        public int MinVersion => 52;
        public string Name => "add hidden to match_pool";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE match_pool
                    ADD COLUMN IF NOT EXISTS hidden boolean DEFAULT false;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
