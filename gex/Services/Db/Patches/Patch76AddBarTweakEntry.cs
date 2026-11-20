using gex.Common.Services.Db;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch76AddBarTweakEntry : IDbPatch {
        public int MinVersion => 76;
        public string Name => "add bar_tweak_entry";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_tweak_entry (
                    hash varchar NOT NULL,
                    day timestamptz NOT NULL,

                    name varchar NOT NULL,
                    base64 varchar NOT NULL,
                    count int NOT NULL,

                    PRIMARY KEY (hash, day)
                );

                CREATE TABLE IF NOT EXISTS bar_tweak_entry_needs_update (
                    day timestamptz NOT NULL PRIMARY KEY,
                    last_dirtied timestamptz NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
