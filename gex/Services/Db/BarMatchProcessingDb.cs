﻿using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

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

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_processing (
                    game_id, demofile_fetched, demofile_parsed, headless_ran, actions_parsed
                ) VALUES (
                    @GameID, @DemofileFetched, @DemofileParsed, @HeadlessRan, @ActionsParsed
                ) ON CONFLICT (game_id) DO UPDATE 
                    SET demofile_fetched = @DemofileFetched,
                        demofile_parsed = @DemofileParsed,
                        headless_ran = @HeadlessRan,
                        actions_parsed = @ActionsParsed;
            ");

            cmd.AddParameter("GameID", proc.GameID);
            cmd.AddParameter("DemofileFetched", proc.ReplayDownloaded);
            cmd.AddParameter("DemofileParsed", proc.ReplayParsed);
            cmd.AddParameter("HeadlessRan", proc.ReplaySimulated);
            cmd.AddParameter("ActionsParsed", proc.ActionsParsed);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<BarMatchProcessing?> GetByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return await conn.QueryFirstOrDefaultAsync<BarMatchProcessing>(
                "SELECT * FROM bar_match_processing WHERE game_id = @GameID",
                 new { GameID = gameID }
            );
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
        public async Task<List<BarMatchProcessing>> GetPending() {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return (await conn.QueryAsync<BarMatchProcessing>(
                @"
                    SELECT
                        mp.* 
                    FROM 
                        bar_match_processing mp
                    WHERE 
                        mp.demofile_fetched is null
                        OR mp.demofile_parsed is null
                        OR (
                            mp.game_id IN (
                                SELECT game_id FROM bar_match_player group by game_id HAVING COUNT(*) = 2
                            )
                            AND mp.headless_ran IS NULL
                        )
                        OR (mp.actions_parsed IS NULL AND mp.headless_ran IS NOT NULL)
                    ;
                "
            )).ToList();
        }

    }
}
