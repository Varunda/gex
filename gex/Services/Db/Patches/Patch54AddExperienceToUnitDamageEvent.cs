using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch54AddExperienceToUnitDamageEvent : IDbPatch {

        public int MinVersion => 54;

        public string Name => "add experience to game_event_unit_damage";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.EVENT);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_unit_damage
                    ADD COLUMN IF NOT EXISTS experience double precision NOT NULL DEFAULT -1;

                ALTER TABLE game_event_unit_damage
                    ALTER COLUMN experience DROP DEFAULT;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
