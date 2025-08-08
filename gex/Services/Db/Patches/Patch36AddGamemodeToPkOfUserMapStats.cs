using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch36AddGamemodeToPkOfUserMapStats : IDbPatch {
        public int MinVersion => 36;
        public string Name => "add gamemode to primary key of user map//faction stats";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_user_map_stats
                    DROP CONSTRAINT IF EXISTS bar_user_map_stats_pkey;

                ALTER TABLE bar_user_map_stats
                    ADD PRIMARY KEY (user_id, map, gamemode);

                ALTER TABLE bar_user_faction_stats
                    DROP CONSTRAINT IF EXISTS bar_user_faction_stats_pkey;

                ALTER TABLE bar_user_faction_stats
                    ADD PRIMARY KEY (user_id, faction, gamemode);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
