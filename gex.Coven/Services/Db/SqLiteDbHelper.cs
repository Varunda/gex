using gex.Common.Services.Db;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db {

    public class SqLiteDbHelper : IDbHelper {

        private SqliteConnection _Connection;

        public SqLiteDbHelper() {
            _Connection = new SqliteConnection("Data Source=gex.db;");
            _Connection.Open();
        }

        public async Task<DbCommand> Command(DbConnection connection, string text, CancellationToken cancel = default) {
            if (connection.State != ConnectionState.Open) {
                await connection.OpenAsync(cancel);
            }

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = text;

            return cmd;
        }

        public DbConnection Connection(string server = "gex", string? task = null, bool enlist = true) {
            if (server == Dbs.MAIN || server == SqLiteDb.READ) {
                DbConnection conn = new SqliteConnection("Data Source=gex.db;Mode=ReadOnly;");
                conn.Open();
                return conn;
            } else if (server == SqLiteDb.WRITE) {
                if (_Connection.State != ConnectionState.Open) {
                    _Connection.Open();
                }
                return _Connection;
            }

            throw new InvalidOperationException($"invalid server passed: '{server}'");
        }

    }
}
