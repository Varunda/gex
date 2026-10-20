using gex.Common.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Models.UserStats;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.UserStats {

    public class BarUserSkillPercentileDb {

        private readonly ILogger<BarUserSkillPercentileDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarUserSkillPercentileDb(ILogger<BarUserSkillPercentileDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get the stored <see cref="BarUserSkillPercentile"/>s in the DB
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarUserSkillPercentile>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarUserSkillPercentile>(
                $"SELECT * FROM bar_user_skill_percentile",
                cancel
            );
        }

        /// <summary>
        ///     generate a new set of <see cref="BarUserSkillPercentile"/> from the current user data
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Generate(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                BEGIN TRANSACTION;
        
                TRUNCATE bar_user_skill_percentile;

                INSERT INTO bar_user_skill_percentile (
                    gamemode, timestamp,
                    p00, p05, p10, p15, p20, p25, p30, p35, p40, p45, p50, p55,
                    p60, p65, p70, p75, p80, p85, p90, p95, p98, p99, p100
                )
                SELECT 
                    gamemode,
                    NOW() at time zone 'utc',
                    percentile_cont(0.00) within group (order by skill asc) ""p0"",
                    percentile_cont(0.05) within group (order by skill asc) ""p5"",
                    percentile_cont(0.10) within group (order by skill asc) ""p10"",
                    percentile_cont(0.15) within group (order by skill asc) ""p15"",
                    percentile_cont(0.20) within group (order by skill asc) ""p20"",
                    percentile_cont(0.25) within group (order by skill asc) ""p25"",
                    percentile_cont(0.30) within group (order by skill asc) ""p30"",
                    percentile_cont(0.35) within group (order by skill asc) ""p35"",
                    percentile_cont(0.40) within group (order by skill asc) ""p40"",
                    percentile_cont(0.45) within group (order by skill asc) ""p45"",
                    percentile_cont(0.50) within group (order by skill asc) ""p50"",
                    percentile_cont(0.55) within group (order by skill asc) ""p55"",
                    percentile_cont(0.60) within group (order by skill asc) ""p60"",
                    percentile_cont(0.65) within group (order by skill asc) ""p65"",
                    percentile_cont(0.70) within group (order by skill asc) ""p70"",
                    percentile_cont(0.75) within group (order by skill asc) ""p75"",
                    percentile_cont(0.80) within group (order by skill asc) ""p80"",
                    percentile_cont(0.85) within group (order by skill asc) ""p85"",
                    percentile_cont(0.90) within group (order by skill asc) ""p90"",
                    percentile_cont(0.95) within group (order by skill asc) ""p95"",
                    percentile_cont(0.98) within group (order by skill asc) ""p98"",
                    percentile_cont(0.99) within group (order by skill asc) ""p99"",
                    percentile_cont(1.00) within group (order by skill asc) ""p100""
                FROM bar_user_skill 
                WHERE skill_uncertainty < 5 AND gamemode <> 0
                GROUP BY gamemode;

                COMMIT;
            ", cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
