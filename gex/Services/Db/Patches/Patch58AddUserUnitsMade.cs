using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch58AddUserUnitsMade : IDbPatch {
        public int MinVersion => 58;
        public string Name => "add user_units_made";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS user_units_made_needs_update (
                    user_id bigint NOT NULL,
                    map_filename varchar NOT NULL,
                    gamemode smallint NOT NULL,
                    day timestamptz NOT NULL,

                    last_dirtied timestamptz NOT NULL,

                    PRIMARY KEY (user_id, map_filename, gamemode, day)
                );

                CREATE TABLE IF NOT EXISTS user_units_made (
                    user_id bigint NOT NULL,
                    map_filename varchar NOT NULL,
                    gamemode smallint NOT NULL,
                    day timestamptz NOT NULL,
                    
                    definition_name varchar NOT NULL,
                    count int NOT NULL,

                    timestamp timestamptz NOT NULL,

                    PRIMARY KEY (user_id, map_filename, gamemode, day, definition_name)
                );

                -- CREATE INDEX IF NOT EXISTS idx_user_units_made_user_id ON user_units_made (user_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
