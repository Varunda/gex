using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BarMatchDb {

        private readonly ILogger<BarMatchDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatch> _Reader;

        public BarMatchDb(ILogger<BarMatchDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatch> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task Insert(BarMatch match, CancellationToken cancel) {
            if (string.IsNullOrEmpty(match.ID)) {
                throw new ArgumentException($"ID of match is empty!");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match (
                    id, start_time, map, duration_ms, engine, game_version, file_name, map_name,
                    host_settings, game_settings, map_settings, spads_settings, restrictions
                ) VALUES (
                    @ID, @StartTime, @Map, @DurationMs, @Engine, @GameVersion, @FileName, @MapName,
                    @HostSettings, @GameSettings, @MapSettings, @SpadsSettings, @Restrictions
                );
            ", cancel);

            cmd.AddParameter("ID", match.ID);
            cmd.AddParameter("StartTime", match.StartTime);
            cmd.AddParameter("Map", match.Map);
            cmd.AddParameter("DurationMs", match.DurationMs);
            cmd.AddParameter("Engine", match.Engine);
            cmd.AddParameter("GameVersion", match.GameVersion);
            cmd.AddParameter("FileName", match.FileName);
            cmd.AddParameter("MapName", match.MapName);
            cmd.AddParameter("HostSettings", match.HostSettings);
            cmd.AddParameter("GameSettings", match.GameSettings);
            cmd.AddParameter("MapSettings", match.MapSettings);
            cmd.AddParameter("SpadsSettings", match.SpadsSettings);
            cmd.AddParameter("Restrictions", match.Restrictions);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task<BarMatch?> GetByID(string ID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync();

            BarMatch? match = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return match;
        }

        /// <summary>
        ///     get recent matches
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetRecent(int offset, int limit) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                SELECT *
                    FROM bar_match
                    ORDER BY start_time DESC
                    LIMIT {limit}
                    OFFSET {offset}
            ");

            await cmd.PrepareAsync();

            List<BarMatch> matches = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return matches;
        }

        public async Task<List<BarMatch>> GetByTimePeriod(DateTime start, DateTime end) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                SELECT *
                    FROM bar_match
                    WHERE start_time > @Start
                        AND start_time <= @End;
            ");

            cmd.AddParameter("Start", start);
            cmd.AddParameter("End", end);
            await cmd.PrepareAsync();

            List<BarMatch> matches = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return matches;
        }

        public async Task Delete(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match
                    WHERE id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
