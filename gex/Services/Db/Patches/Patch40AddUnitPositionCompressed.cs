using Npgsql;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch40AddUnitPositionCompressed : IDbPatch {
        public int MinVersion => 40;
        public string Name => "add unit_position_compressed to bar_match_processing";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_processing
                    ADD COLUMN IF NOT EXISTS unit_position_compressed boolean NOT NULL DEFAULT false;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
