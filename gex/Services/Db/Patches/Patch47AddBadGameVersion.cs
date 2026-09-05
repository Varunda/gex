using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch47AddBadGameVersion : IDbPatch {
        public int MinVersion => 47;
        public string Name => "add bad_game_version table";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bad_game_version (
                    game_version varchar NOT NULL PRIMARY KEY
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
