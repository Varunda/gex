using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch67AddCompletedToUnitCreated : IDbPatch {
        public int MinVersion => 67;
        public string Name => "add completed to game_event_unit_created";

        public async Task Execute(IDbHelper helper) {
            {
                using DbConnection conn = helper.Connection(Dbs.EVENT);
                using DbCommand cmd = await helper.Command(conn, @"
                    ALTER TABLE game_event_unit_created
                        ADD COLUMN IF NOT EXISTS completed int NOT NULL DEFAULT 0;
                ");

                await cmd.ExecuteNonQueryAsync();
                await conn.CloseAsync();
            }

            {
                using DbConnection conn = helper.Connection(Dbs.MAIN);
                using DbCommand cmd = await helper.Command(conn, @"
                    ALTER TABLE bar_match_ally_team
                        ADD COLUMN IF NOT EXISTS average_skill numeric NOT NULL DEFAULT -1;

                    ALTER TABLE bar_match_ally_team
                        ALTER COLUMN average_skill DROP DEFAULT;
                ");

                /*
					UPDATE bar_match_ally_team
						SET average_skill = sq.avg
					FROM (
						SELECT 
                            at1.game_id, at1.ally_team_id, avg(skill)
                        FROM 
                            bar_match_ally_team at1
                            INNER JOIN bar_match_player mp1 ON mp1.game_id = at1.game_id AND mp1.ally_team_id = at1.ally_team_id
                        GROUP BY 
                            at1.game_id, at1.ally_team_id
					) sq
					WHERE 
                        sq.game_id = bar_match_ally_team.game_id 
                        AND sq.ally_team_id = bar_match_ally_team.ally_team_id;
                 */

                await cmd.ExecuteNonQueryAsync();
                await conn.CloseAsync();
            }

            using DbConnection conn2 = helper.Connection(Dbs.MAIN);

        }

    }
}
