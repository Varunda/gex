using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch60AddStartPositionDataToMap : IDbPatch {
        public int MinVersion => 60;
        public string Name => "add start_position_data to bar_map";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_map
                    ADD COLUMN IF NOT EXISTS start_position_data jsonb NULL;

                ALTER TABLE bar_map
                    ADD COLUMN IF NOT EXISTS timestamp timestamptz NOT NULL DEFAULT (NOW() at time zone 'utc');

                ALTER TABLE bar_map
                    ADD COLUMN IF NOT EXISTS symmetry_axis int NULL;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
