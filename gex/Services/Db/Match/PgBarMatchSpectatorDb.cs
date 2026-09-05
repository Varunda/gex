using Dapper;
using gex.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gex.Common.Services.Db.Match;

namespace gex.Services.Db.Match {

    public class PgBarMatchSpectatorDb : IBarMatchSpectatorDb {

        private readonly ILogger<PgBarMatchSpectatorDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public PgBarMatchSpectatorDb(ILogger<PgBarMatchSpectatorDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(BarMatchSpectator spec) {
            if (string.IsNullOrEmpty(spec.GameID)) {
                throw new ArgumentException($"missing GameID from spectator");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_spectator (
                    game_id, player_id, user_id, user_name, user_id_can_be_wrong
                ) VALUES (
                    @GameID, @PlayerID, @UserID, @Username, @UserIDCanBeWrong
                );
            ");

            cmd.AddParameter("GameID", spec.GameID);
            cmd.AddParameter("PlayerID", spec.PlayerID);
            cmd.AddParameter("UserID", spec.UserID);
            cmd.AddParameter("Username", spec.Name);
            cmd.AddParameter("UserIDCanBeWrong", spec.UserIDCanBeWrong);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchSpectator>> GetByGameID(string gameID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<BarMatchSpectator>(new CommandDefinition(
                "SELECT * FROM bar_match_spectator WHERE game_id = @GameID",
                new { GameID = gameID },
                cancellationToken: cancel
            ))).ToList();
        }

        public async Task DeleteByGameID(string gameID) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_spectator
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
