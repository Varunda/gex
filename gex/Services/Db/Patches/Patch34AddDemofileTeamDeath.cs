using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch34AddDemofileTeamDeath : IDbPatch {
        public int MinVersion => 34;
        public string Name => "add demofile_team_death";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_match_team_death (
                    game_id varchar NOT NULL,
                    team_id smallint NOT NULL,
                    reason smallint NOT NULL,
                    game_time float NOT NULL,

                    PRIMARY KEY (game_id, team_id)
                );

                CREATE INDEX IF NOT EXISTS idx_bar_match_team_death_game_id ON bar_match_team_death (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
