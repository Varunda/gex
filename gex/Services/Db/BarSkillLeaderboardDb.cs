using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BarSkillLeaderboardDb {

        private readonly ILogger<BarSkillLeaderboardDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarSkillLeaderboardDb(ILogger<BarSkillLeaderboardDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarSkillLeaderboardEntry>> Get(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarSkillLeaderboardEntry>(@"
				(select gamemode, s.user_id, u.username, skill from bar_user_skill s left join bar_user u ON s.user_id = u.id where gamemode = 1 order by skill desc limit 10)
				UNION
				(select gamemode, s.user_id, u.username, skill from bar_user_skill s left join bar_user u ON s.user_id = u.id where gamemode = 2 order by skill desc limit 10)
				UNION
				(select gamemode, s.user_id, u.username, skill from bar_user_skill s left join bar_user u ON s.user_id = u.id where gamemode = 3 order by skill desc limit 10)
				UNION
				(select gamemode, s.user_id, u.username, skill from bar_user_skill s left join bar_user u ON s.user_id = u.id where gamemode = 4 order by skill desc limit 10)
				UNION
				(select gamemode, s.user_id, u.username, skill from bar_user_skill s left join bar_user u ON s.user_id = u.id where gamemode = 5 order by skill desc limit 10)
				order by gamemode asc, skill desc;
			", cancel);
        }

    }
}
