using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch24AddMapPriorityMod : IDbPatch {
        public int MinVersion => 24;
        public string Name => "add map_priority_mod";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS map_priority_mod (
					map_name varchar NOT NULL PRIMARY KEY,
					change smallint NOT NULL,
					timestamp timestamptz NOT NULL
				);
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
