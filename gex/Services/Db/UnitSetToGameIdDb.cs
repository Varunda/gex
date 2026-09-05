using Dapper;
using gex.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class UnitSetToGameIdDb {

        private readonly ILogger<UnitSetToGameIdDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public UnitSetToGameIdDb(ILogger<UnitSetToGameIdDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(GameIdToUnitDefHash entry) {
            if (string.IsNullOrEmpty(entry.GameID)) {
                throw new System.Exception($"missing GameID from entry");
            }
            if (string.IsNullOrEmpty(entry.Hash)) {
                throw new System.Exception($"missing Hash from entry");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                INSERT INTO game_id_to_unit_def_hash (
                    game_id, hash
                ) VALUES (
                    @GameID, @Hash
                ) ON CONFLICT (game_id) DO UPDATE
                    SET hash = @Hash;
            ");

            cmd.AddParameter("GameID", entry.GameID);
            cmd.AddParameter("Hash", entry.Hash);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<GameIdToUnitDefHash?> GetByGameID(string gameID) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);

            GameIdToUnitDefHash? entry = await conn.QueryFirstOrDefaultAsync<GameIdToUnitDefHash?>(
                "SELECT * FROM game_id_to_unit_def_hash WHERE game_id = @GameID",
                new { GameID = gameID }
            );

            return entry;
        }


    }
}
