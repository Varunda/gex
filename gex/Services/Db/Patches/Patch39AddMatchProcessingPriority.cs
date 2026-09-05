using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch39AddMatchProcessingPriority : IDbPatch {
        public int MinVersion => 39;
        public string Name => "add bar_match_processing_priority";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_match_processing_priority (
                    discord_id bigint NOT NULL PRIMARY KEY,
                    game_id varchar NOT NULL,
                    timestamp timestamptz NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
