using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch66AddMatchPorcessingWebhook : IDbPatch {
        public string Name => "add match_processing_webhook";
        public int MinVersion => 66;

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS match_processing_webhook (
                    url varchar NOT NULL,
                    type varchar NOT NULL,

                    shared_secret varchar NOT NULL,
                    include_events boolean NOT NULL,
                    timestamp timestamptz NOT NULL,
                    ip varchar NOT NULL,

                    PRIMARY KEY (url, type)
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
