using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch05AddMapNameToReplay : IDbPatch {
        public int MinVersion => 5;
        public string Name => "add map_name to bar_repaly";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_replay ADD COLUMN IF NOT EXISTS map_name varchar NOT NULL DEFAULT '';
                ALTER TABLE bar_match ADD COLUMN IF NOT EXISTS map_name varchar NOT NULL DEFAULT '';
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
