using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchProcessingDb {

        private readonly ILogger<BarMatchProcessingDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchProcessingDb(ILogger<BarMatchProcessingDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Upsert(BarMatchProcessing proc) {
            if (string.IsNullOrEmpty(proc.GameID)) {
                throw new System.Exception($"missing GameID from bar match processing entry");
            }

            _Logger.LogTrace($"upserting match processing [gameID={proc.GameID}] [priority={proc.Priority}]");

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_processing (
                    game_id, demofile_fetched, demofile_parsed, headless_ran, actions_parsed,
                    fetch_ms, parse_ms, replay_ms, action_ms,
					priority, unit_position_compressed
                ) VALUES (
                    @GameID, @DemofileFetched, @DemofileParsed, @HeadlessRan, @ActionsParsed,
                    @FetchMs, @ParseMs, @ReplayMs, @ActionMs,
					@Priority, @UnitPositionCompressed
                ) ON CONFLICT (game_id) DO UPDATE 
                    SET demofile_fetched = @DemofileFetched,
                        demofile_parsed = @DemofileParsed,
                        headless_ran = @HeadlessRan,
                        actions_parsed = @ActionsParsed,
                        fetch_ms = @FetchMs,
                        parse_ms = @ParseMs,
                        replay_ms = @ReplayMs,
                        action_ms = @ActionMs,
						priority = @Priority,
                        unit_position_compressed = @UnitPositionCompressed
                ;
            ");

            cmd.AddParameter("GameID", proc.GameID);
            cmd.AddParameter("DemofileFetched", proc.ReplayDownloaded);
            cmd.AddParameter("DemofileParsed", proc.ReplayParsed);
            cmd.AddParameter("HeadlessRan", proc.ReplaySimulated);
            cmd.AddParameter("ActionsParsed", proc.ActionsParsed);
            cmd.AddParameter("FetchMs", proc.ReplayDownloadedMs);
            cmd.AddParameter("ParseMs", proc.ReplayParsedMs);
            cmd.AddParameter("ReplayMs", proc.ReplaySimulatedMs);
            cmd.AddParameter("ActionMs", proc.ActionsParsedMs);
            cmd.AddParameter("Priority", proc.Priority);
            cmd.AddParameter("UnitPositionCompressed", proc.UnitPositionCompressed);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        /// <summary>
        ///		load the <see cref="BarMatchProcessing"/> for a specific game
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///		the <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/> of <paramref name="gameID"/>,
        ///		or <c>null</c> if it does not exist in the database
        /// </returns>
        public async Task<BarMatchProcessing?> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return await conn.QueryFirstOrDefaultAsync<BarMatchProcessing>(new CommandDefinition(
                "SELECT * FROM bar_match_processing WHERE game_id = @GameID",
                 new { GameID = gameID },
                 cancellationToken: cancel
            ));
        }

        /// <summary>
        ///     get a list of <see cref="BarMatchProcessing"/> entries that indicate that further processing
        ///     needs to be done. see remarks for what conditions must be met to indicate this
        /// </summary>
        /// <remarks>
        ///     a <see cref="BarMatchProcessing"/> needs to be further processed if one of the following conditions is met:
        ///     <list>
        ///         <item><see cref="BarMatchProcessing.ReplayDownloaded"/> is null</item>
        ///         <item><see cref="BarMatchProcessing.ReplayParsed"/> is null</item>
        ///         <item><see cref="BarMatchProcessing.ReplaySimulated"/> is null, and there are 2 players in the game</item>
        ///         <item><see cref="BarMatchProcessing.ActionsParsed"/> is null, and <see cref="BarMatchProcessing.ReplaySimulated"/> is not null</item>
        ///     </list>
        /// </remarks>
        public async Task<List<BarMatchProcessing>> GetPending(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return (await conn.QueryAsync<BarMatchProcessing>(new CommandDefinition(
                @"
                    SELECT
                        mp.* 
                    FROM 
                        bar_match_processing mp
                    WHERE 
                        mp.demofile_fetched is null
                        OR mp.demofile_parsed is null
                        OR (
							mp.priority = -1
                            AND mp.headless_ran IS NULL
                        )
                        OR (mp.actions_parsed IS NULL AND mp.headless_ran IS NOT NULL)
                    ;
                ",
                cancellationToken: cancel
            ))).ToList();
        }

        public async Task<BarMatchProcessing?> GetLowestPriority(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return await conn.QueryFirstOrDefaultAsync<BarMatchProcessing>(new CommandDefinition(
                @"
                    WITH mods AS (
                        select game_id, count(*) * 20 ""mod"" from bar_match_processing_priority group by game_id
                    )
                    SELECT
                        mp.game_id, 
                        mp.demofile_fetched, mp.demofile_parsed, mp.headless_ran, mp.actions_parsed,
                        mp.fetch_ms, mp.parse_ms, mp.replay_ms, mp.action_ms,
                        GREATEST(1, priority - COALESCE(c.mod, 0::bigint)) ""priority""
                    FROM 
                        bar_match_processing mp
                        INNER JOIN bar_match m ON m.id = mp.game_id
                        LEFT JOIN mods c ON mp.game_id = c.game_id
                    WHERE 
                        priority > 0
                        AND mp.demofile_fetched is not null
                        AND mp.demofile_parsed is not null
                        AND mp.headless_ran IS NULL
                    ORDER BY
                        GREATEST(1, priority - COALESCE(c.mod, 0::bigint)) ASC, 
                        m.start_time DESC
                    LIMIT 1;
                ",
                cancellationToken: cancel
            ));
        }

        /// <summary>
        ///		get a list of priority processing games
        /// </summary>
        /// <param name="count">how many games to list. anything above 1'000 will throw an exception</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public async Task<List<BarMatchProcessing>> GetPriorityList(int count, CancellationToken cancel) {
            if (count > 1000) {
                throw new System.Exception($"failsafe, only allowing a max of 1000 for count, got {count}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return await conn.QueryListAsync<BarMatchProcessing>(@$"
                WITH mods AS (
                    select game_id, count(*) * 20 ""mod"" from bar_match_processing_priority group by game_id
                )
                SELECT
                    mp.game_id, 
                    mp.demofile_fetched, mp.demofile_parsed, mp.headless_ran, mp.actions_parsed,
                    mp.fetch_ms, mp.parse_ms, mp.replay_ms, mp.action_ms,
                    priority - COALESCE(c.mod, 0::bigint) ""priority""
                FROM 
                    bar_match_processing mp
                    INNER JOIN bar_match m ON m.id = mp.game_id
                    LEFT JOIN mods c ON mp.game_id = c.game_id
                WHERE 
                    priority > 0
                    AND mp.demofile_fetched is not null
                    AND mp.demofile_parsed is not null
                    AND mp.headless_ran IS NULL
                ORDER BY
                    priority - COALESCE(c.mod, 0::bigint) ASC, 
                    m.start_time DESC
                LIMIT {count};
            ", cancellationToken: cancel);
        }

        /// <summary>
        ///     get all <see cref="BarMatchProcessing"/> where 
        ///     <see cref="BarMatchProcessing.UnitPositionCompressed"/> is false
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<BarMatchProcessing>> NeedsUnitPositionCompression(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchProcessing>(@"
                SELECT p.*
                FROM bar_match_processing p
                WHERE p.unit_position_compressed IS false
                    AND p.actions_parsed IS NOT NULL;
            ", cancel);
        }

    }
}
