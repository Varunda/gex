using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch48AddMapEngineUsage : IDbPatch {
        public int MinVersion => 48;
        public string Name => "add map_engine_usage table";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS map_engine_usage (
					engine varchar NOT NULL,
					map varchar NOT NULL,
					last_used timestamptz NOT NULL,
					deleted_on timestamptz NULL,

                    PRIMARY KEY (engine, map)
                );

                INSERT INTO map_engine_usage (
                    engine, map , last_used
                ) SELECT engine, map, max(start_time)
                    FROM bar_match
                    GROUP BY engine, map
                ON CONFLICT (engine, map) DO NOTHING;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
