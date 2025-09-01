using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch38AddLobbyAlertTable : IDbPatch {
        public int MinVersion => 38;
        public string Name => "add lobby_alert table";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand comm = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS lobby_alert (
                    ID bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    guild_id bigint NOT NULL,
                    channel_id bigint NOT NULL,
                    role_id bigint NULL,
                    created_by_id bigint NOT NULL,
                
                    time_between_alerts_seconds int NOT NULL,
                    timestamp timestamptz NOT NULL,

                    map varchar NULL,
        
                    minimum_os int NULL,
                    maximum_os int NULL,

                    minimum_average_os int NULL,
                    maximum_average_os int NULL,

                    minimum_player_count int NULL,
                    maximum_player_count int NULL,
                    
                    gamemode smallint NULL
                );
            ");

            await comm.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
