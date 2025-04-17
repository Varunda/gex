using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

	public class GameVersionUsageDb {

		private readonly ILogger<GameVersionUsageDb> _Logger;
		private readonly IDbHelper _DbHelper;

		public GameVersionUsageDb(ILogger<GameVersionUsageDb> logger,
			IDbHelper dbHelper) {

			_Logger = logger;
			_DbHelper = dbHelper;
		}

		/// <summary>
		///		update/insert (upsert) a <see cref="GameVersionUsage"/>
		/// </summary>
		/// <param name="entry">entry to update</param>
		/// <param name="cancel"></param>
		/// <returns></returns>
		public async Task Upsert(GameVersionUsage entry, CancellationToken cancel) {
			if (string.IsNullOrEmpty(entry.Engine) == true) {
				throw new Exception($"missing {nameof(GameVersionUsage.Engine)} from {nameof(GameVersionUsage)}");
			}
			if (string.IsNullOrEmpty(entry.Version) == true) {
				throw new Exception($"missing {nameof(GameVersionUsage.Version)} from {nameof(GameVersionUsage)}");
			}

			_Logger.LogDebug($"updating game version usage [engine={entry.Engine}] [version={entry.Version}] [lastUsed={entry.LastUsed:u}]");

			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
				INSERT INTO game_version_usage AS v (
					engine, version, last_used, deleted_on
				) VALUES (
					@Engine, @Version, @LastUsed, @DeletedOn
				) ON CONFLICT (engine, version) DO UPDATE SET
					last_used = @LastUsed,
					deleted_on = @DeletedOn
                WHERE v.last_used < @LastUsed;
			", cancel);

			cmd.AddParameter("Engine", entry.Engine);
			cmd.AddParameter("Version", entry.Version);
			cmd.AddParameter("LastUsed", entry.LastUsed);
			cmd.AddParameter("DeletedOn", entry.DeletedOn);
			await cmd.PrepareAsync(cancel);

			await cmd.ExecuteNonQueryAsync(cancel);
			await conn.CloseAsync();
		}

		/// <summary>
		///		mark a game version as deleted
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="version"></param>
		/// <param name="when"></param>
		/// <param name="cancel"></param>
		/// <returns></returns>
		public async Task MarkDeleted(string engine, string version, DateTime when, CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
				UPDATE game_version_usage
					SET deleted_on = @DeletedOn
				WHERE
					engine = @Engine
					AND version = @Version;
            ", cancel);

			cmd.AddParameter("DeletedOn", when);
			cmd.AddParameter("Engine", engine);
			cmd.AddParameter("Version", version);
			await cmd.PrepareAsync(cancel);

			await cmd.ExecuteNonQueryAsync(cancel);
			await conn.CloseAsync();
		}

		/// <summary>
		///		get all game versions that haven't been used in over a day. used for cleaning up old game data
		/// </summary>
		/// <param name="cancel"></param>
		/// <returns>
		///		a list of <see cref="GameVersionUsage"/> where <see cref="GameVersionUsage.LastUsed"/>
		///		is over 1 day ago
		/// </returns>
		public async Task<List<GameVersionUsage>> GetExpired(CancellationToken cancel) {
			using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
			return (await conn.QueryAsync<GameVersionUsage>(new CommandDefinition(
				"SELECT * FROM game_version_usage WHERE deleted_on IS NULL AND last_used < NOW() AT TIME ZONE 'utc' - '1 day'::INTERVAL",
				cancellationToken: cancel
			))).ToList();
		}

	}
}
