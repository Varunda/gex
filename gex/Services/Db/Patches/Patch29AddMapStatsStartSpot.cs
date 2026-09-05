using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch29AddMapStatsStartSpot : IDbPatch {
        public int MinVersion => 29;
        public string Name => "add map_stats_start_spot";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS map_stats_start_spot (
					map_file_name varchar NOT NULL,
					gamemode smallint NOT NULL,
					start_x int NOT NULL,
					start_z int NOT NULL,
					count_total int NOT NULL,
					count_win int NOT NULL,
					timestamp timestamptz NOT NULL,

					PRIMARY KEY (map_file_name, gamemode, start_x, start_z)
				);
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
