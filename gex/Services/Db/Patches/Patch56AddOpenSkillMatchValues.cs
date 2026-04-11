
using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch56AddOpenSkillMatchValues : IDbPatch {

        public int MinVersion => 56;
        public string Name => "add *_os values to bar_match";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match
                    ADD COLUMN IF NOT EXISTS average_os numeric NOT NULL DEFAULT 0;

                ALTER TABLE bar_match
                    ADD COLUMN IF NOT EXISTS min_os numeric NOT NULL DEFAULT 0;

                ALTER TABLE bar_match
                    ADD COLUMN IF NOT EXISTS max_os numeric NOT NULL DEFAULT 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
            
            /*
             * query to update the min_os, max_os and average_os values:

                with os_values AS (
                    select game_id, min(skill) "min_os", max(skill) "max_os", avg(skill) "average_os"
                    from bar_match_player
                    group by game_id
                )
                UPDATE bar_match m
                    SET min_os = os_values.min_os,
                        max_os = os_values.max_os,
                        average_os = os_values.average_os
                    FROM os_values
                    WHERE id = os_values.game_id;
             */
        }

    }
}
