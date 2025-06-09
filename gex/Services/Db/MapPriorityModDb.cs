using Dapper;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class MapPriorityModDb {

        private readonly ILogger<MapPriorityModDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapPriorityModDb(ILogger<MapPriorityModDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///		get all <see cref="MapPriorityMod"/> stored in the DB
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<MapPriorityMod>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<MapPriorityMod>(new CommandDefinition(@"
				SELECT * FROM map_priority_mod
			", cancellationToken: cancel
            ))).ToList();
        }

        public async Task<MapPriorityMod?> GetByName(string name, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryFirstOrDefaultAsync<MapPriorityMod>(new CommandDefinition(
                @"SELECT * FROM map_priority_mod WHERE map_name = @Name",
                new { Name = name },
                cancellationToken: cancel
            ));
        }

    }
}
