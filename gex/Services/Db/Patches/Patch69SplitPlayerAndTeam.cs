using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch69SplitPlayerAndTeam : IDbPatch {
        public int MinVersion => 69;
        public string Name => "split bar_match_player and create bar_match_team";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_match_team (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,

                    game_id varchar NOT NULL,
                    team_id int NOT NULL,
                    
                    ally_team_id int NOT NULL,
                    team_leader_id int NOT NULL,
                    
                    faction varchar NOT NULL,
                    starting_position_x numeric NOT NULL,
                    starting_position_y numeric NOT NULL,
                    starting_position_z numeric NOT NULL,
                    color int NOT NULL,
                    handicap numeric NOT NULL,

                    start_spot varchar NULL,
                    start_spot_label varchar NULL,

                    CONSTRAINT unq_bar_match_team_game_id_team_id UNIQUE (game_id, team_id)
                );

                CREATE INDEX IF NOT EXISTS idx_team_game_id ON bar_match_team (game_id);

                ALTER TABLE bar_match_player
                    DROP COLUMN IF EXISTS faction,
                    DROP COLUMN IF EXISTS starting_position_x,
                    DROP COLUMN IF EXISTS starting_position_y,
                    DROP COLUMN IF EXISTS starting_position_z,
                    DROP COLUMN IF EXISTS color,
                    DROP COLUMN IF EXISTS handicap,
                    DROP COLUMN IF EXISTS start_spot,
                    DROP COLUMN IF EXISTS start_spot_label;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
