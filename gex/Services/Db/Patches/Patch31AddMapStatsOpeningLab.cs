using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch31AddMapStatsOpeningLab : IDbPatch {
        public int MinVersion => 31;
        public string Name => "add map_stats_opening_lab";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE OR REPLACE VIEW game_event_unit_created_def AS (
					SELECT uc.*, ud.definition_name 
					FROM game_event_unit_created uc 
						LEFT JOIN game_id_to_unit_def_hash h ON h.game_id = uc.game_id
						LEFT JOIN unit_def_set_entry ud ON h.hash = ud.hash AND uc.definition_id = ud.definition_id
				);

				CREATE TABLE IF NOT EXISTS map_stats_opening_lab (
					map_file_name varchar NOT NULL,
					gamemode smallint NOT NULL,
					def_name varchar NOT NULL,

					timestamp timestamptz NOT NULL,

					count_total int NOT NULL,
					count_win int NOT NULL,

					PRIMARY KEY (map_file_name, gamemode, def_name)
				);
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
