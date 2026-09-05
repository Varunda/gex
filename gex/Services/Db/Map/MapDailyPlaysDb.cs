using gex.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class MapDailyPlaysDb {

        private readonly ILogger<MapDailyPlaysDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MapDailyPlaysDb(ILogger<MapDailyPlaysDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MapDailyPlays>> GetByMap(string mapFilename, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MapDailyPlays>(
                @"
                    SELECT 
                        map_name ""map_name"",
                        date_trunc('day', start_time) ""day"",
                        count(*) ""count""
                    FROM 
                        bar_match 
                    WHERE map_name = @MapFilename
                    GROUP BY 1, 2 
                    ORDER BY 2 asc;
                ",
                new { MapFilename = mapFilename },
                cancel
            );
        }

    }
}
