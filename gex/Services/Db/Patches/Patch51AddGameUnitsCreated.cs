
using Npgsql;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch51AddGameUnitsCreated : IDbPatch {
        public int MinVersion => 51;
        public string Name => "add game_units_created";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.EVENT);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS game_units_created (
                    game_id varchar NOT NULL,
                    team_id smallint NOT NULL,
                    user_id bigint NOT NULL,
                    definition_name varchar NOT NULL,
                    count int NOT NULL,
                    timestamp timestamptz NOT NULL,

                    PRIMARY KEY (game_id, user_id, definition_name)
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
