using gex.Code.ExtensionMethods;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

	public class MapStatsByFactionDb {

		private readonly ILogger<MapStatsByFactionDb> _Logger;
		private readonly IDbHelper _DbHelper;

		public MapStatsByFactionDb(ILogger<MapStatsByFactionDb> logger,
			IDbHelper dbHelper) {

			_Logger = logger;
			_DbHelper = dbHelper;
		}

		public async Task<List<MapStatsByFaction>> GetByMap(string mapFilename, CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			return await conn.QueryListAsync<MapStatsByFaction>(
				"SELECT * FROM map_stats_by_faction WHERE map_file_name = @MapFileName",
				new { MapFileName = mapFilename },
				cancel
			);
		}

		public async Task Generate(string mapFilename, CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
				BEGIN TRANSACTION;

				DELETE FROM map_stats_by_faction WHERE map_file_name = @MapFileName;
				
				INSERT INTO map_stats_by_faction (
					map_file_name, gamemode, faction, timestamp,
					play_count_all_time, win_count_all_time,
					play_count_month, win_count_month,
					play_count_week, win_count_week,
					play_count_day, win_count_day
				) SELECT 
					m.map_name ""map_file_name"",
					m.gamemode ""gamemode"",
					CASE
						WHEN p.faction = 'Armada' THEN 1
						WHEN p.faction = 'Cortex' THEN 2 
						WHEN p.faction = 'Legion' THEN 3
						WHEN p.faction = 'Random' THEN 4
					END ""faction"",
					
					NOW() at time zone 'utc' ""timestamp"",
					
					count(*) ""play_count_all_time"",
					count(*) filter (where at.won = true) ""win_count_all_time"",
					
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval)) ""play_count_month"",
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval) AND at.won = true) ""win_count_month"",
					
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval)) ""play_count_week"",
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval) AND at.won = true) ""win_count_week"",
					
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval)) ""play_count_day"",
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval) AND at.won = true) ""win_count_day""
				FROM
					bar_match m
					LEFT JOIN bar_match_player p ON p.game_id = m.id
					LEFT JOIN bar_match_ally_team at ON p.ally_team_id = at.ally_team_id AND p.game_id = at.game_id
				WHERE
					m.map_name = @MapFileName
					AND m.gamemode <> 0
				GROUP BY m.map_name, m.gamemode, p.faction;

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
