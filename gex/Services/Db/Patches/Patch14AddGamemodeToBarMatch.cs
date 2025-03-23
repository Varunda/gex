using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch14AddGamemodeToBarMatch : IDbPatch {
        public int MinVersion => 14;
        public string Name => "add gamemode to bar_match";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match ADD COLUMN IF NOT EXISTS gamemode smallint NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
