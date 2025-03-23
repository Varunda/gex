using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.UserStats;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.UserStats {

    public class BarUserSkillDb {

        private readonly ILogger<BarUserSkillDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarUserSkillDb(ILogger<BarUserSkillDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     upsert (update/insert) a <see cref="BarUserSkill"/> with new data
        /// </summary>
        /// <param name="skill">parameters used to insert or update</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for the async operation</returns>
        public async Task Upsert(BarUserSkill skill, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_user_skill (
                    user_id, gamemode, skill, skill_uncertainty, last_updated
                ) VALUES (
                    @UserID, @Gamemode, @Skill, @SkillUncertainty, @LastUpdated
                ) ON CONFLICT (user_id, gamemode) DO UPDATE SET
                    skill = @Skill,
                    skill_uncertainty = @SkillUncertainty,
                    last_updated = @LastUpdated;
            ", cancel);

            cmd.AddParameter("UserID", skill.UserID);
            cmd.AddParameter("Gamemode", skill.Gamemode);
            cmd.AddParameter("Skill", skill.Skill);
            cmd.AddParameter("SkillUncertainty", skill.SkillUncertainty);
            cmd.AddParameter("LastUpdated", skill.LastUpdated);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     get the <see cref="BarUserSkill"/> entries of a user
        /// </summary>
        /// <param name="userID">ID of the user</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of <see cref="BarUserSkill"/> with <see cref="BarUserSkill.UserID"/>
        ///     of <paramref name="userID"/>
        /// </returns>
        public async Task<List<BarUserSkill>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<BarUserSkill>(new CommandDefinition(
                "SELECT * FROM bar_user_skill WHERE user_id = @UserID",
                new { UserID = userID },
                cancellationToken: cancel
            ))).ToList();
        }

    }
}
