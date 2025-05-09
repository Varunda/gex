using gex.Code.ExtensionMethods;
using gex.Models.MapStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.MapStats {

	public class MapStatsDb {

		private readonly ILogger<MapStatsDb> _Logger;
		private readonly IDbHelper _DbHelper;

		public MapStatsDb(ILogger<MapStatsDb> logger,
			IDbHelper dbHelper) {

			_Logger = logger;
			_DbHelper = dbHelper;
		}

		/// <summary>
		///		get the <see cref="MapStatsByGamemode"/> by a map
		/// </summary>
		/// <param name="mapFilename"></param>
		/// <param name="cancel"></param>
		/// <returns></returns>
		public async Task<List<MapStatsByGamemode>> GetByMap(string mapFilename, CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			return await conn.QueryListAsync<MapStatsByGamemode>(
				"SELECT * FROM map_stats WHERE map_file_name = @MapFileName ORDER BY gamemode ASC;",
				new { MapFileName = mapFilename },
				cancel
			);
		}

		public async Task Generate(string mapFilename, CancellationToken cancel) {

			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
				BEGIN TRANSACTION;

				DELETE FROM map_stats WHERE map_file_name = @MapFileName;
				
				INSERT INTO map_stats (
					map_file_name, gamemode, timestamp,
					play_count_day, play_count_week, play_count_month, play_count_all_time,
					duration_average_ms, duration_median_ms
				) SELECT 
					filename ""map_file_name"",
					m.gamemode ""gamemode"",
					NOW() at time zone 'utc' ""timestamp"",
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 day'::interval)) ""play_count_day"",
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 week'::interval)) ""play_count_week"",
					count(*) filter (where m.start_time >= (NOW() at time zone 'utc' - '1 month'::interval)) ""play_count_month"",
					count(*) ""play_count_all_time"",
					avg(duration_ms) ""duration_average_ms"",
					percentile_cont(0.5) WITHIN GROUP(ORDER BY duration_ms) ""duration_median_ms""
				FROM
					bar_map map
					LEFT JOIN bar_match m ON m.map_name = map.filename
				WHERE
					m.map_name = @MapFileName
					AND m.gamemode <> 0
				GROUP BY map.filename, m.gamemode;

				COMMIT TRANSACTION;
			", cancel);

			cmd.AddParameter("MapFileName", mapFilename);
			await cmd.PrepareAsync(cancel);

			await cmd.ExecuteNonQueryAsync(cancel);
			await conn.CloseAsync();
		}

	}
}
