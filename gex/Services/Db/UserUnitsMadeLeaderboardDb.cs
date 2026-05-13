using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Models.UserStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class UserUnitsMadeLeaderboardDb {

        private readonly ILogger<UserUnitsMadeLeaderboardDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public UserUnitsMadeLeaderboardDb(ILogger<UserUnitsMadeLeaderboardDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<UserUnitsMadeLeaderboardEntry>> Get(UserUnitsMadeLeaderboardOptions options, CancellationToken cancel) {
            if (options.Offset < 0) {
                throw new ArgumentException($"{nameof(UserUnitsMadeLeaderboardOptions.Offset)} must be at least 0");
            }
            if (options.Limit > 1000) {
                throw new ArgumentException($"{nameof(UserUnitsMadeLeaderboardOptions.Limit)} must be 1000 or lower");
            }

            List<string> conds = [];

            if (options.MapFilename != null && options.MapFilename.Count > 0) {
                conds.Add("uum.map_filename = ANY(@MapFilenames)");
            }
            if (options.Gamemodes != null && options.Gamemodes.Count > 0) {
                conds.Add("uum.gamemode = ANY(@Gamemodes)");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            string query = @$"
                WITH top_units AS (
                    SELECT 
                        uum.user_id,
                        sum(count) ""count""
                    FROM 
                        user_units_made uum
                    WHERE
                        uum.definition_name = ANY(@UnitDefs)
                        AND uum.day >= @PeriodStart
                        AND uum.day < @PeriodEnd
                        AND {(conds.Count > 0 ? string.Join("\n AND ", conds) : "1=1")}
                    GROUP BY uum.user_id
                    ORDER BY 2 DESC
                    OFFSET {options.Offset}
                    LIMIT {options.Limit}
                )
                SELECT
                    tu.user_id, 
                    u.username,
                    tu.count
                FROM
                    top_units tu
                    LEFT JOIN bar_user u ON u.id = tu.user_id
                ORDER BY count desc;
            ";

            return await conn.QueryListAsync<UserUnitsMadeLeaderboardEntry>(
                query,
                new {
                    UnitDefs = options.UnitDefinitions,
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd,
                    MapFilenames = options.MapFilename ?? [],
                    Gamemodes = options.Gamemodes ?? []
                },
                cancel
            );
        }

    }
}
