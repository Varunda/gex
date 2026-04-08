using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch55AddActionsCompressed : IDbPatch {
        public int MinVersion => 55;
        public string Name => "add actions_compressed to bar_match_processing";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_processing
                    ADD COLUMN IF NOT EXISTS actions_compressed timestamptz NULL;

                ALTER TABLE bar_match_processing
                    ADD COLUMN IF NOT EXISTS actions_compressed_ms int NULL;

                ALTER TABLE bar_match_processing
                    ADD COLUMN IF NOT EXISTS actions_deleted timestamptz NULL;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
