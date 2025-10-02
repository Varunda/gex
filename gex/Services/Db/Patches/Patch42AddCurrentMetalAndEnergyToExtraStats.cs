using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch42AddCurrentMetalAndEnergyToExtraStats : IDbPatch {
        public int MinVersion => 42;
        public string Name => "add current_metal and current_energy to extra stats";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_extra_stats
                    ADD COLUMN IF NOT EXISTS metal_current double precision NOT NULL DEFAULT 0;

                ALTER TABLE game_event_extra_stats
                    ADD COLUMN IF NOT EXISTS energy_current double precision NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }

}
