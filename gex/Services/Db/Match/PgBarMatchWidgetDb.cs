using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class PgBarMatchPlayerWidgetDb : IBarMatchPlayerWidgetDb {

        private readonly ILogger<PgBarMatchPlayerWidgetDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public PgBarMatchPlayerWidgetDb(ILogger<PgBarMatchPlayerWidgetDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <inheritdoc/>
        public async Task<List<BarMatchPlayerWidget>> GetByGameID(string gameID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchPlayerWidget>(
                $"SELECT * FROM bar_match_player_widget WHERE game_id = @GameID;",
                new { GameID = gameID },
                cancel
            );
        }

        /// <inheritdoc/>
        public async Task<List<BarMatchPlayerWidget>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchPlayerWidget>(
                $"SELECT * FROM bar_match_player_widget WHERE game_id = ANY(@GameIDs);",
                new { GameIDs = gameIDs },
                cancel
            );
        }

        /// <inheritdoc/>
        public async Task Insert(BarMatchPlayerWidget widget, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(widget.GameID) == true) {
                throw new ArgumentException($"{nameof(BarMatchPlayerWidget.GameID)} is missing");
            }
            if (string.IsNullOrWhiteSpace(widget.Hash) == true) {
                throw new ArgumentException($"{nameof(BarMatchPlayerWidget.Hash)} is missing");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_player_widget (
                    game_id, user_id, hash, state
                ) VALUES (
                    @GameID, @UserID, @Hash, @State
                );
            ", cancel);

            cmd.AddParameter("GameID", widget.GameID);
            cmd.AddParameter("UserID", widget.UserID);
            cmd.AddParameter("Hash", widget.Hash);
            cmd.AddParameter("State", widget.State);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteByGameID(string gameID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_player_widget WHERE game_id = @GameID;
            ", cancel);

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
