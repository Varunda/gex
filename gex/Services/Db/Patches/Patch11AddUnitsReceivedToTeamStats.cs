using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch11AddUnitsReceivedToTeamStats : IDbPatch {

        public int MinVersion => 11;
        public string Name => "add units_received to game_event_team_stats";

        public async Task Execute(IDbHelper helper) {

            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_team_stats
                    ADD COLUMN IF NOT EXISTS units_received int NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
