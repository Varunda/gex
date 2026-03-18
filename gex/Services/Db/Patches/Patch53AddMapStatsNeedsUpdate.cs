
using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch53AddMapStatsNeedsUpdate : IDbPatch {

        public int MinVersion => 53;
        public string Name => "add map_stats_needs_update";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS map_stats_needs_update (
                    map_filename varchar NOT NULL,
                    gamemode smallint NOT NULL,
                    day timestamptz NOT NULL,

                    last_dirtied timestamptz NOT NULL,

                    PRIMARY KEY (map_filename, gamemode, day)
                );

                CREATE TABLE IF NOT EXISTS map_stats_daily_opening_lab (
                    map_filename varchar NOT NULL,
                    gamemode smallint NOT NULL,
                    day timestamptz NOT NULL,
            
                    definition_name varchar NOT NULL,
                    count int NOT NULL,
                    wins int NOT NULL,

                    timestamp timestamptz NOT NULL
                );

                CREATE TABLE IF NOT EXISTS map_stats_daily_units_made (
                    map_filename varchar NOT NULL,
                    gamemode smallint NOT NULL,
                    day timestamptz NOT NULL,
                    
                    definition_name varchar NOT NULL,
                    count int NOT NULL,

                    timestamp timestamptz NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
