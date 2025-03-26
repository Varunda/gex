using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch16UpdateExtraStats : IDbPatch {
        public int MinVersion => 16;

        public string Name => "update game_event_army_value to game_event_extra_stats";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE IF EXISTS game_event_army_value_update RENAME TO game_event_extra_stats;

                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS build_power_available double precision NOT NULL default 0;

                ALTER TABLE game_event_extra_stats ADD COLUMN IF NOT EXISTS build_power_used double precision NOT NULL default 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            if ((await helper.HasColumn("game_event_extra_stats", "value")) == true) {
                using NpgsqlConnection conn2 = helper.Connection(Dbs.MAIN);
                using NpgsqlCommand cmd2 = await helper.Command(conn, @"
                    ALTER TABLE game_event_extra_stats RENAME COLUMN value TO army_value;
                ");

                await cmd2.ExecuteNonQueryAsync();
                await conn2.CloseAsync();
            }

        }
    }
}
