using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch59AddHideUntilToMatchPool : IDbPatch {
        public int MinVersion => 59;
        public string Name => "add hide_until to match_pool";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE match_pool
                    ADD COLUMN IF NOT EXISTS hide_until timestamptz NULL;

                ALTER TABLE match_pool
                    RENAME COLUMN hidden TO unlisted;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
