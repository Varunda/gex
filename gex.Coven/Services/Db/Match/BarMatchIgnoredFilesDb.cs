using gex.Common.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Coven.Models.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match {

    public class BarMatchIgnoredFilesDb {

        private readonly ILogger<BarMatchIgnoredFilesDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchIgnoredFilesDb(ILogger<BarMatchIgnoredFilesDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarMatchIgnoredFile>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QueryListAsync<BarMatchIgnoredFile>("SELECT * FROM bar_match_ignored_files;", cancel);
        }

        public async Task Insert(BarMatchIgnoredFile ignore, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(ignore.FileName) == true) {
                throw new ArgumentNullException(nameof(BarMatchIgnoredFile.FileName));
            }

            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_ignored_files (
                    filename, reason
                ) VALUES (
                    @FileName, @Reason
                );
            ", cancel);

            cmd.AddParameter("FileName", ignore.FileName);
            cmd.AddParameter("Reason", ignore.Reason);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
        }

        public async Task Remove(string filename, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_ignored_files
                    WHERE filename = @FileName;
            ", cancel);

            cmd.AddParameter("FileName", filename);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
        }

    }
}
