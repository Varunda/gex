using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch13AddUserStats : IDbPatch {
        public int MinVersion => 13;

        public string Name => "add user stats tables";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_user (
                    id bigint NOT NULL PRIMARY KEY,
                    username varchar NOT NULL,
                    last_updated timestamptz NOT NULL
                );

                CREATE TABLE IF NOT EXISTS bar_user_skill (
                    user_id bigint NOT NULL,
                    gamemode smallint NOT NULL,
                    skill double precision NOT NULL,
                    skill_uncertainty double precision NOT NULL,
                    last_updated timestamptz NOT NULL,

                    PRIMARY KEY (user_id, gamemode)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_user_skill_user_id ON bar_user_skill (user_id);

                CREATE TABLE IF NOT EXISTS bar_user_faction_stats (
                    user_id bigint NOT NULL,
                    faction smallint NOT NULL,
                    gamemode smallint NOT NULL,
                    play_count int NOT NULL,
                    win_count int NOT NULL,
                    loss_count int NOT NULL,
                    tie_count int NOT NULL,
                    last_updated timestamptz NOT NULL,
            
                    PRIMARY KEY (user_id, faction)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_user_faction_stats_user_id ON bar_user_faction_stats (user_id);

                CREATE TABLE IF NOT EXISTS bar_user_map_stats (
                    user_id bigint NOT NULL,
                    map varchar NOT NULL,
                    gamemode smallint NOT NULL,
                    play_count int NOT NULL,
                    win_count int NOT NULL,
                    loss_count int NOT NULL,
                    tie_count int NOT NULL,
                    last_updated timestamptz NOT NULL,
            
                    PRIMARY KEY (user_id, map)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_user_map_stats ON bar_user_map_stats (user_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
