using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

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
                    id, start_time, map, duration_ms, engine, game_version, file_name, map_name, gamemode,
                    host_settings, game_settings, map_settings, spads_settings, restrictions
                ) VALUES (
                    @ID, @StartTime, @Map, @DurationMs, @Engine, @GameVersion, @FileName, @MapName, @Gamemode,
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
            cmd.AddParameter("Gamemode", match.Gamemode);

            cmd.AddParameter("HostSettings", match.HostSettings);
            cmd.AddParameter("GameSettings", match.GameSettings);
            cmd.AddParameter("MapSettings", match.MapSettings);
            cmd.AddParameter("SpadsSettings", match.SpadsSettings);
            cmd.AddParameter("Restrictions", match.Restrictions);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     get a single <see cref="BarMatch"/> by it's <see cref="BarMatch.ID"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<BarMatch?> GetByID(string ID, CancellationToken cancel) {
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
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetRecent(int offset, int limit, CancellationToken cancel) {
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

        /// <summary>
        ///     get a list of <see cref="BarMatch"/>s that took place between a period of time
        /// </summary>
        /// <param name="start">start of the time period to include (exclusive)</param>
        /// <param name="end">end of the time period to include (inclusive)</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetByTimePeriod(DateTime start, DateTime end, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                SELECT *
                    FROM bar_match
                    WHERE start_time > @Start
                        AND start_time <= @End;
            ");

            cmd.AddParameter("Start", start);
            cmd.AddParameter("End", end);
            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
        }

        /// <summary>
        ///     get all matches of a user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                SELECT * FROM bar_match WHERE id IN (
                    SELECT distinct(game_id) FROM bar_match_player WHERE user_id = @UserID
                ) ORDER BY start_time DESC;
            ");

            cmd.AddParameter("UserID", userID);
            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
        }

        /// <summary>
        ///     delete a <see cref="BarMatch"/> from the DB
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
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
