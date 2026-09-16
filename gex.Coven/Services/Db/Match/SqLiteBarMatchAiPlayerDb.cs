using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match {

    public class SqLiteBarMatchAiPlayerDb : IBarMatchAiPlayerDb {

        private readonly ILogger<SqLiteBarMatchAiPlayerDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public SqLiteBarMatchAiPlayerDb(ILogger<SqLiteBarMatchAiPlayerDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public Task<List<BarMatchAiPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return conn.QueryListAsync<BarMatchAiPlayer>("SELECT * FROM bar_match_ai_player WHERE game_id = @GameID;",
                new { GameID = gameID },
                cancel
            );
        }

        public Task<List<BarMatchAiPlayer>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public async Task Insert(BarMatchAiPlayer ai, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_ai_player (
                    game_id, ai_id, team_id, name
                ) VALUES (
                    @GameID, @AiID, @TeamID, @Name
                );
            ", cancel);

            cmd.AddParameter("GameID", ai.GameID);
            cmd.AddParameter("AiID", ai.AiID);
            cmd.AddParameter("TeamID", ai.TeamID);
            cmd.AddParameter("Name", ai.Name);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
        }

        public Task DeleteByGameID(string gameID) {
            throw new NotImplementedException();
        }

    }
}
