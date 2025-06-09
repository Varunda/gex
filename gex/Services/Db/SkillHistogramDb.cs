using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class SkillHistogramDb {

        private readonly ILogger<SkillHistogramDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public SkillHistogramDb(ILogger<SkillHistogramDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<SkillHistogramEntry>> Get(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<SkillHistogramEntry>(@"
				select 
					trunc(skill) ""skill"",
					count(distinct(s.user_id)) ""count""
				from 
					bar_user_skill s
					inner join bar_user_faction_stats fs ON s.user_id = fs.user_id
				where 
					s.gamemode IN (1, 2, 3)
					AND s.skill <> 16.67
				group by 1
				having sum(play_count) > 25
				order by 1 asc;
			", cancel);
        }

    }
}
