using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch35AddDiscordSubscriptionMatchProcessed : IDbPatch {
        public int MinVersion => 35;
        public string Name => "add discord_subscription_match_processed";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS discord_subscription_match_processed (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    user_id bigint NOT NULL,
                    discord_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,

                    CONSTRAINT unq_discord_subscription_match_processed_user_discord UNIQUE (user_id, discord_id)
                );

                CREATE INDEX IF NOT EXISTS idx_discord_subscription_match_processed_user_id ON discord_subscription_match_processed (user_id);
                CREATE INDEX IF NOT EXISTS idx_discord_subscription_match_processed_discord_id ON discord_subscription_match_processed (discord_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
