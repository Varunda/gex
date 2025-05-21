using gex.Code.ExtensionMethods;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

	public class MapStatsOpeningLabDb {

		private readonly ILogger<MapStatsOpeningLabDb> _Logger;
		private readonly IDbHelper _DbHelper;

		public MapStatsOpeningLabDb(ILogger<MapStatsOpeningLabDb> logger,
			IDbHelper dbHelper) {

			_Logger = logger;
			_DbHelper = dbHelper;
		}

		public async Task<List<MapStatsOpeningLab>> GetByMap(string mapFilename, CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			return await conn.QueryListAsync<MapStatsOpeningLab>(
				"SELECT * FROM map_stats_opening_lab WHERE map_file_name = @MapFilename",
				new { MapFilename = mapFilename },
				cancel
			);
		}

		public async Task Generate(string mapFilename, CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
				BEGIN TRANSACTION;

				DELETE FROM map_stats_opening_lab WHERE map_file_name = @MapFileName;

				WITH first_lab AS (
					SELECT
						bm.id,
						geucd.team_id,
						json_agg(geucd.definition_name order by frame)->>0 ""defname""
					FROM
						bar_match bm
						left join game_event_unit_created_def geucd on bm.id = geucd.game_id
					WHERE
						map_name = @MapFileName
						and geucd.definition_name in (
							'armlab', 'armvp', 'armap', 'armsy', 'armhp', 'armfhp',
							'corlab', 'corvp', 'corap', 'corsy', 'corhp', 'corfhp',
							'leglab', 'legvp', 'legap', 'legsy', 'leghp', 'legfhp'
						)
					GROUP BY
						bm.id, geucd.team_id
				)
				INSERT INTO map_stats_opening_lab (
					map_file_name, gamemode, def_name,
					timestamp,
					count_total, count_win
				) SELECT
					m.map_name,
					m.gamemode,
					fl.defname,
					NOW() at time zone 'utc',
					count(*) ""count_total"",
					count(*) filter (where at.won = true) ""count_win""
				FROM
					first_lab fl
					left join bar_match m ON fl.id = m.id
					left join bar_match_player p ON fl.id = p.game_id AND fl.team_id = p.team_id
					left join bar_match_ally_team at ON p.game_id = at.game_id AND p.ally_team_id = at.ally_team_id
				WHERE m.gamemode <> 0
				GROUP BY 1, 2, 3;

				COMMIT TRANSACTION;
			", cancel);

			cmd.CommandTimeout = 60 * 5; // 5 minutes

			cmd.AddParameter("MapFileName", mapFilename);
			await cmd.PrepareAsync(cancel);

			await cmd.ExecuteNonQueryAsync(cancel);
			await conn.CloseAsync();
		}

	}
}
