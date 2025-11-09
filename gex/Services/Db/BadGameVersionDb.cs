using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BadGameVersionDb {

        private readonly ILogger<BadGameVersionDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BadGameVersionDb(ILogger<BadGameVersionDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<BadGameVersion?> GetByGameVersion(string gameVersion, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<BadGameVersion>(
                "SELECT * FROM bad_game_version WHERE game_version = @GameVersion",
                new { GameVersion = gameVersion},
                cancel
            );
        }

    }
}
