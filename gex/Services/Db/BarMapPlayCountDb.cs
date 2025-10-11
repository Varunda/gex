using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BarMapPlayCountDb {

        private readonly ILogger<BarMapPlayCountDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMapPlayCountDb(ILogger<BarMapPlayCountDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarMapPlayCountEntry>> Get(DateTime rangeStart, CancellationToken cancel) {
            if (rangeStart > DateTime.UtcNow) {
                throw new Exception($"{nameof(rangeStart)} cannot be in the future");
            }

            if ((DateTime.UtcNow - rangeStart) > TimeSpan.FromDays(31)) {
                throw new Exception($"{nameof(rangeStart)} can be at most 31 days ago");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMapPlayCountEntry>(@"
                WITH matches AS (
                    select gamemode, map, count(*) from bar_match 
                    WHERE start_time >= @RangeStart
                    GROUP BY gamemode, map
                )
                (select gamemode, map, count from matches WHERE gamemode = 1 order by count desc limit 4)
                UNION
                (select gamemode, map, count from matches WHERE gamemode = 2 order by count desc limit 4)
                UNION
                (select gamemode, map, count from matches WHERE gamemode = 3 order by count desc limit 4)
                UNION
                (select gamemode, map, count from matches WHERE gamemode = 4 order by count desc limit 4)
                UNION
                (select gamemode, map, count from matches WHERE gamemode = 5 order by count desc limit 4)
                order by gamemode asc, count desc;
            ", new { RangeStart = rangeStart }, cancel);
        }

    }
}
