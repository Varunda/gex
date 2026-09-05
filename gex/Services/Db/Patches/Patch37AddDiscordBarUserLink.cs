using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch37AddDiscordBarUserLink : IDbPatch {
        public int MinVersion => 37;
        public string Name => "add discord_bar_user_link";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS discord_bar_user_link (
                    discord_id bigint NOT NULL PRIMARY KEY,
                    bar_user_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
