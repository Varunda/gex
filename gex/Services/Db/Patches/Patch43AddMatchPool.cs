using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch43AddMatchPool : IDbPatch {
        public int MinVersion => 43;
        public string Name => "add match pool tables";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS match_pool (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    name varchar NOT NULL,
                    created_by_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL
                );

                CREATE TABLE IF NOT EXISTS match_pool_entry (
                    pool_id bigint NOT NULL,
                    match_id varchar NOT NULL,
                    added_by_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,

                    PRIMARY KEY (pool_id, match_id)
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
