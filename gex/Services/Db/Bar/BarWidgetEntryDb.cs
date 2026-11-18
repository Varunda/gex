using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Bar;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Bar {

    public class BarWidgetEntryDb {

        private readonly ILogger<BarWidgetEntryDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarWidgetEntryDb(ILogger<BarWidgetEntryDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarWidgetEntry>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarWidgetEntry>($"SELECT * FROM bar_widget_entry", cancel);
        }

        public async Task Insert(BarWidgetEntry entry, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(entry.Hash)) {
                throw new ArgumentException($"{nameof(BarWidgetEntry.Hash)} is missing");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_widget_entry (
                    hash, name, author, description, timestamp
                ) VALUES (
                    @Hash, @Name, @Author, @Description, NOW() at time zone 'utc'
                );
            ", cancel);

            cmd.AddParameter("Hash", entry.Hash);
            cmd.AddParameter("Name", entry.Name);
            cmd.AddParameter("Author", entry.Author);
            cmd.AddParameter("Description", entry.Description);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
