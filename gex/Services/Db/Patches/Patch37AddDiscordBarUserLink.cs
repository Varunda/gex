using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch37AddDiscordBarUserLink : IDbPatch {
        public int MinVersion => 37;
        public string Name => "add discord_bar_user_link";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
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
