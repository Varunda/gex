using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch57AddDescriptionToMatchPoolEntry : IDbPatch {

        public int MinVersion => 57;
        public string Name => "add description to match_pool_entry";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE match_pool_entry
                    ADD COLUMN IF NOT EXISTS description varchar NULL;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
