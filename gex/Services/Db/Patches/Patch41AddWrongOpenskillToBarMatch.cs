using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch41AddWrongOpenskillToBarMatch : IDbPatch {
        public int MinVersion => 41;
        public string Name => "addd wrong_skill_values to bar_match";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match
                    ADD COLUMN IF NOT EXISTS wrong_skill_values bool NOT NULL DEFAULT false;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }

}
