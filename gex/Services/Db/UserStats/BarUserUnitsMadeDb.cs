using gex.Code.ExtensionMethods;
using gex.Models.UserStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.UserStats {

    public class BarUserUnitsMadeDb {

        private readonly ILogger<BarUserUnitsMadeDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarUserUnitsMadeDb(ILogger<BarUserUnitsMadeDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get the <see cref="BarUserUnitsMade"/>s for a specific user
        /// </summary>
        /// <param name="userID">ID of the user to get the unit create count for</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarUserUnitsMade>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarUserUnitsMade>(@"
                    SELECT
                        mp.user_id ""user_id"",
                        date_trunc('day', m.start_time) ""date"",
                        ud.name ""unit_name"",
                        ud.definition_name ""definition_name"",
                        count(*) ""count""
                    from
                        bar_match_player mp
                        INNER JOIN bar_match m ON mp.game_id = m.id
                        LEFT JOIN game_event_unit_created c ON mp.game_id = c.game_id AND mp.team_id = c.team_id
                        INNER JOIN game_id_to_unit_def_hash h ON h.game_id = c.game_id
                        LEFT JOIN unit_def_set_entry ud ON h.hash = ud.hash AND c.definition_id = ud.definition_id
                    WHERE
                        mp.user_id = @UserID
                    GROUP BY mp.user_id, 2, ud.name, ud.definition_name;
                ",
                new { UserID = userID },
                cancel
            );
        }

    }
}
