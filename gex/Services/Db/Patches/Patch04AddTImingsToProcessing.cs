using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch04AddTimingsToProcessing : IDbPatch {

        public int MinVersion => 4;

        public string Name => "add timings to bar_match_processing";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_processing ADD COLUMN IF NOT EXISTS fetch_ms int null;
                ALTER TABLE bar_match_processing ADD COLUMN IF NOT EXISTS parse_ms int null;
                ALTER TABLE bar_match_processing ADD COLUMN IF NOT EXISTS replay_ms int null;
                ALTER TABLE bar_match_processing ADD COLUMN IF NOT EXISTS action_ms int null;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
