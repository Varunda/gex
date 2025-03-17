using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    public class Patch07AddRotationToUnitCreated : IDbPatch {

        public int MinVersion => 7;

        public string Name => "add rotation to unit created";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_unit_created ADD COLUMN IF NOT EXISTS rotation double precision NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
