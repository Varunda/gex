using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch46AddActionsToExtraStats : IDbPatch {
        public int MinVersion => 46;
        public string Name => "add actions to extra_stats";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_extra_stats
                    ADD COLUMN IF NOT EXISTS actions int NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
