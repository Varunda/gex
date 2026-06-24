using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch64AddFeaturesToBarMatchProcessing : IDbPatch {
        public int MinVersion => 64;
        public string Name => "add features to bar_match_processing";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_processing
                    ADD COLUMN IF NOT EXISTS features varchar;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
