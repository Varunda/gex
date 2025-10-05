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

        /// <summary>
        ///		insert a new <see cref="BarMatch"/>
        /// </summary>
        /// <param name="match">match to insert. <see cref="BarMatch.ID"/> must be populated</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task Insert(BarMatch match, CancellationToken cancel) {
            if (string.IsNullOrEmpty(match.ID)) {
                throw new ArgumentException($"ID of match is empty!");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match (
                    id, start_time, map, duration_ms, duration_frame_count,
					engine, game_version, file_name, map_name, gamemode, player_count, uploaded_by, wrong_skill_values,
                    host_settings, game_settings, map_settings, spads_settings, restrictions
                ) VALUES (
                    @ID, @StartTime, @Map, @DurationMs, @DurationFrameCount,
					@Engine, @GameVersion, @FileName, @MapName, @Gamemode, @PlayerCount, @UploadedBy, @WrongSkillValues,
                    @HostSettings, @GameSettings, @MapSettings, @SpadsSettings, @Restrictions
                );
            ", cancel);

            cmd.AddParameter("ID", match.ID);
            cmd.AddParameter("StartTime", match.StartTime);
            cmd.AddParameter("Map", match.Map);
            cmd.AddParameter("DurationMs", match.DurationMs);
            cmd.AddParameter("DurationFrameCount", match.DurationFrameCount);
            cmd.AddParameter("Engine", match.Engine);
            cmd.AddParameter("GameVersion", match.GameVersion);
            cmd.AddParameter("FileName", match.FileName);
            cmd.AddParameter("MapName", match.MapName);
            cmd.AddParameter("Gamemode", match.Gamemode);
            cmd.AddParameter("PlayerCount", match.PlayerCount);
            cmd.AddParameter("UploadedBy", match.UploadedByID);
            cmd.AddParameter("WrongSkillValues", match.WrongSkillValues);

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
        ///     update just the <see cref="BarMatch.WrongSkillValues"/> of a <see cref="BarMatch"/>
        /// </summary>
        /// <param name="match">match to update. uses <see cref="BarMatch.ID"/> and <see cref="BarMatch.WrongSkillValues"/></param>
        /// <param name="cancel">cancellation token</param>
        public async Task UpdateWrongSkillValues(BarMatch match, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE bar_match
                    SET wrong_skill_values = @WrongSkillValues
                    WHERE id = @ID;
            ", cancel);

            cmd.AddParameter("WrongSkillValues", match.WrongSkillValues);
            cmd.AddParameter("ID", match.ID);
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
            ", cancel);

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync(cancel);

            BarMatch? match = await _Reader.ReadSingle(cmd, cancel);
            await conn.CloseAsync();

            return match;
        }

        /// <summary>
        ///     get a list of <see cref="BarMatch"/>
        /// </summary>
        /// <param name="IDs">List of IDs to get from the DB</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetByIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match
                    WHERE id = ANY(@IDs);
            ", cancel);

            cmd.AddParameter("IDs", IDs);
            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
        }
        
        /// <summary>
        ///     get all <see cref="BarMatch"/>s in the DB. do not use this frequently
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                SELECT * FROM bar_match;
            ", cancel);

            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
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
            ", cancel);

            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
        }

        /// <summary>
        ///		perform a search in the DB based on the parameters passed in <paramref name="parms"/>
        /// </summary>
        /// <param name="parms">parameters used to search</param>
        /// <param name="offset">offset into the search</param>
        /// <param name="limit">limit the returned results</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///		a list of <see cref="BarMatch"/>s that fulfill the search parameters
        /// </returns>
        public async Task<List<BarMatch>> Search(BarMatchSearchParameters parms, int offset, int limit, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, ""); // command text will be set later

            List<string> conditions = [];

            bool joinProcessing = false;
            bool joinPool = false;

            if (parms.EngineVersion != null) {
                conditions.Add("m.engine = @Engine");
                cmd.AddParameter("Engine", parms.EngineVersion);
            }

            if (parms.GameVersion != null) {
                conditions.Add("m.game_version = @GameVersion");
                cmd.AddParameter("GameVersion", parms.GameVersion);
            }

            if (parms.Map != null) {
                conditions.Add("m.map = @Map");
                cmd.AddParameter("Map", parms.Map);
            }

            if (parms.StartTimeAfter != null) {
                conditions.Add("m.start_time > @StartTime");
                cmd.AddParameter("StartTime", parms.StartTimeAfter.Value);
            }

            if (parms.StartTimeBefore != null) {
                conditions.Add("m.start_time <= @EndTime");
                cmd.AddParameter("EndTime", parms.StartTimeBefore.Value);
            }

            if (parms.DurationMinimum != null) {
                conditions.Add("m.duration_ms > @DurationMin");
                cmd.AddParameter("DurationMin", parms.DurationMinimum.Value);
            }

            if (parms.DurationMaximum != null) {
                conditions.Add("m.duration_ms <= @DurationMax");
                cmd.AddParameter("DurationMax", parms.DurationMaximum.Value);
            }

            if (parms.Ranked != null) {
                // 2025-05-09 TODO: why does this return 0 results when used as a query parameter?
                //conditions.Add("m.game_settings->>'ranked_game' = @Ranked");
                //cmd.AddParameter("Ranked", parms.Ranked.Value == true ? "'1'" : "'0'");
                conditions.Add($"m.game_settings->>'ranked_game' = {(parms.Ranked.Value == true ? "'1'" : "'0'")}");
            }

            if (parms.Gamemode != null) {
                conditions.Add("m.gamemode = @Gamemode");
                cmd.AddParameter("Gamemode", parms.Gamemode.Value);
            }

            if (parms.ProcessingDownloaded != null) {
                joinProcessing = true;
                conditions.Add("p.demofile_fetched is "
                    + ((parms.ProcessingDownloaded.Value == true) ? "not null" : "null"));
            }

            if (parms.ProcessingParsed != null) {
                joinProcessing = true;
                conditions.Add("p.demofile_parsed is "
                    + ((parms.ProcessingParsed.Value == true) ? "not null" : "null"));
            }

            if (parms.ProcessingReplayed != null) {
                joinProcessing = true;
                conditions.Add("p.headless_ran is "
                    + ((parms.ProcessingReplayed.Value == true) ? "not null" : "null"));
            }

            if (parms.ProcessingAction != null) {
                joinProcessing = true;
                conditions.Add("p.actions_parsed is "
                    + ((parms.ProcessingAction.Value == true) ? "not null" : "null"));
            }

            if (parms.PlayerCountMinimum != null) {
                conditions.Add("m.player_count > @PlayerCountMin");
                cmd.AddParameter("PlayerCountMin", parms.PlayerCountMinimum.Value);
            }

            if (parms.PlayerCountMaximum != null) {
                conditions.Add("m.player_count <= @PlayerCountMax");
                cmd.AddParameter("PlayerCountMax", parms.PlayerCountMaximum.Value);
            }

            if (parms.LegionEnabled != null) {
                conditions.Add($"m.game_settings->>'experimentallegionfaction' = {(parms.LegionEnabled.Value == true ? "'1'" : "'0'")}");
            }

            if (parms.PoolID != null) {
                conditions.Add("mp.pool_id = @PoolID");
                cmd.AddParameter("PoolID", parms.PoolID.Value);
                joinPool = true;
            }

            cmd.CommandText = $@"
                SELECT *
                    FROM bar_match m
						{((joinProcessing == true) ? "LEFT JOIN bar_match_processing p ON m.id = p.game_id " : "")}
                        {((joinPool == true) ? "INNER JOIN match_pool_entry mp ON mp.match_id = m.id " : "")}
					WHERE 1=1
						AND {(conditions.Count > 0 ? string.Join("\n AND ", conditions) : "1=1")}
                    ORDER BY {parms.OrderBy.Value} {parms.OrderByDirection.Value}
                    LIMIT {limit}
                    OFFSET {offset}
            ";

            _Logger.LogDebug($"performing DB search: " + cmd.Print());

            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
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
            ", cancel);

            cmd.AddParameter("UserID", userID);
            await cmd.PrepareAsync(cancel);

            List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
        }

        /// <summary>
        ///     get the oldest <see cref="BarMatch"/> in the DB, or <c>null</c> if no matches are in the DB
        /// </summary>
        /// <remarks>
        ///     ignores user uploaded matches, as those might be older than when Gex started
        /// </remarks>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<BarMatch?> GetOldestMatch(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT * 
                FROM bar_match 
                WHERE uploaded_by IS NULL 
                ORDER BY start_time ASC
                LIMIT 1;
            ", cancel);

            await cmd.PrepareAsync(cancel);

            BarMatch? match = await _Reader.ReadSingle(cmd, cancel);
            await conn.CloseAsync();

            return match;
        }

        /// <summary>
        ///		get all unique engine versions stored in the DB
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///		a string of all unique values of <see cref="BarMatch.Engine"/> stored in the DB
        /// </returns>
        public async Task<List<string>> GetUniqueEngines(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return (await conn.QueryAsync<string>(new CommandDefinition(
                "SELECT distinct(engine) FROM bar_match",
                cancellationToken: cancel
            ))).ToList();
        }

        /// <summary>
        ///		get all unique <see cref="BarMatch.GameVersion"/>s stored in the DB
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///		a list of all unique values of <see cref="BarMatch.GameVersion"/> stored in the DB
        /// </returns>
        public async Task<List<string>> GetUniqueGameVersions(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return (await conn.QueryAsync<string>(new CommandDefinition(
                "SELECT distinct(game_version) FROM bar_match",
                cancellationToken: cancel
            ))).ToList();
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
