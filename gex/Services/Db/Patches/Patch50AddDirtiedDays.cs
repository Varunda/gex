using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch50AddDirtiedDays : IDbPatch {
        public int MinVersion => 50;
        public string Name => "add map_opening_lab_needs_update";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS map_opening_lab_needs_update (
                    day timestamptz NOT NULL,
                    map_filename varchar NOT NULL,
                    last_dirtied timestamptz NOT NULL,

                    PRIMARY KEY (day, map_filename)
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
