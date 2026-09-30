using gex.Common.Services.Db;
using gex.Coven.Services.Util;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db {

    public class SqLiteDbHelper : IDbHelper {

        private SqliteConnection? _Connection;

        private static string _DbPath = $"\"{ShellUtil.GetWorkingDirectory()}/gex.db\"";

        private readonly SemaphoreSlim _Signal = new(1, 1);

        private readonly ILogger<SqLiteDbHelper>? _Logger = null;

        public SqLiteDbHelper() {
            _Logger = App.Current?.Services?.GetService<ILogger<SqLiteDbHelper>>();
            /*
            _Connection = new SqliteConnection($"Data Source={_DbPath};");
            _Connection.Open();

            _Connection.Disposed += (object? sender, EventArgs args) => {
                _Signal.Release();
                Trace.WriteLine($"");
                StackTrace st = new();
                Trace.WriteLine($"StackTrace of write connection being disposed:\n{st.ToString()}");
                Debug.Fail("why is this being disposed");
            };
            */
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
                DbConnection conn = new SqliteConnection($"Data Source={_DbPath};Mode=ReadOnly;");
                conn.Open();
                return conn;
            } else if (server == SqLiteDb.WRITE) {
#if DEBUG
                StackTrace st = new(true);
                string source = "";
                string trace = st.ToString();
                string[] traces = trace.Split("\n");
                if (traces[0].Trim().StartsWith("at gex.Coven.Services.Db.SqLiteDbHelper.Connection(") && traces.Length > 1) {
                    source = traces[1].Replace("   at ", "");
                }

                if (string.IsNullOrEmpty(source)) {
                    _Logger?.LogTrace($"getting write connection to DB. stack trace:\n{st}");
                } else {
                    _Logger?.LogTrace($"getting write connection to DB. source: {source}");
                }
#endif

                bool aquired = _Signal.Wait(TimeSpan.FromSeconds(10));
                if (aquired == false) {
                    _Logger?.LogWarning($"failed to wait on semaphore to aquire connection lock");
                    throw new TimeoutException($"failed to aquire write connection");
                }

                DbConnection connection = new SqliteConnection($"Data Source={_DbPath}");
                connection.Open();
                connection.Disposed += (object? sender, EventArgs args) => {
#if DEBUG
                    _Logger?.LogTrace($"lock released");
                    Trace.WriteLine($"lock released on write connection for SqLite DB");
#endif
                    _Signal.Release();
                };

                return connection;

                /*
                if (_Connection == null) {
                    _Connection = new SqliteConnection($"Data Source={_DbPath}");
                    _Connection.Open();
                }

                return _Connection;
                */
            }

            throw new InvalidOperationException($"invalid server passed: '{server}'");
        }

    }
}
