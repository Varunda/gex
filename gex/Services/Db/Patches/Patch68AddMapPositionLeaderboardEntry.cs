using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch68AddMapPositionLeaderboardEntry : IDbPatch {
        public int MinVersion => 68;
        public string Name => "add map_position_leaderboard_entry";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS map_position_leaderboard_entry (
                    user_id bigint NOT NULL,
                    map_filename varchar NOT NULL,
                    position_label varchar NOT NULL,

                    score double precision NOT NULL,
                    play_count int NOT NULL,
                    win_count int NOT NULL,
                    average_enemy_skill double precision NOT NULL,

                    timestamp timestamptz NOT NULL,
                
                    PRIMARY KEY (user_id, map_filename, position_label)
                );

                CREATE INDEX IF NOT EXISTS idx_map_position_leaderboard_entry_map ON map_position_leaderboard_entry (map_filename);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
