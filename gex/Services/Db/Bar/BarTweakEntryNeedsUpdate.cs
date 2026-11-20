using gex.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using gex.Models.Bar;

namespace gex.Services.Db.Bar {

    public class BarTweakEntryNeedsUpdateDb {

        private readonly ILogger<BarTweakEntryNeedsUpdateDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarTweakEntryNeedsUpdateDb(ILogger<BarTweakEntryNeedsUpdateDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get all <see cref="UserUnitsMadeNeedsUpdate"/> that are pending generation
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<BarTweakEntryNeedsUpdate>> GetReady(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarTweakEntryNeedsUpdate>(@"
                SELECT *
                FROM bar_tweak_entry_needs_update
                WHERE last_dirtied <= (NOW() at time zone 'utc' - '1 minute'::interval);
            ", cancel);
        }

        /// <summary>
        ///     upsert a <see cref="BarTweakEntryNeedsUpdate"/>
        /// </summary>
        /// <param name="update"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Upsert(BarTweakEntryNeedsUpdate update, CancellationToken cancel) {
            if (update.Day == default) {
                throw new Exception($"missing {nameof(BarTweakEntryNeedsUpdate.Day)}]");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                INSERT INTO bar_tweak_entry_needs_update (
                    day, last_dirtied
                ) VALUES (
                    @Day, @LastDirtied
                ) ON CONFLICT (day) DO UPDATE
                    SET last_dirtied = @LastDirtied;
            ", cancel);

            cmd.AddParameter("Day", update.Day.Date);
            cmd.AddParameter("LastDirtied", update.LastDirtied);

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     remove a <see cref="BarTweakEntryNeedsUpdate"/> from the db, marking it as completed
        /// </summary>
        /// <param name="update"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task Remove(BarTweakEntryNeedsUpdate update, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                DELETE FROM bar_tweak_entry_needs_update
                    WHERE day = @Day;
            ", cancel);

            cmd.AddParameter("Day", update.Day.Date);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
