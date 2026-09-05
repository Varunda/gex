using gex.Common.Services.Db;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Implementations {

    public class PgDbHelper : IDbHelper {

        private readonly ILogger<PgDbHelper> _Logger;
        private readonly IConfiguration _Configuration;

        private static ConcurrentDictionary<string, NpgsqlDataSource> _DataSources = new();

        public PgDbHelper(ILogger<PgDbHelper> logger, IConfiguration config) {
            _Logger = logger;
            _Configuration = config;

            // create all data sources up top, and name them so the otel metrics have nice names (instead of the connection string)
            IConfigurationSection allStrings = _Configuration.GetSection("ConnectionStrings");
            foreach (KeyValuePair<string, string?> conn in allStrings.AsEnumerable()) {
                _Logger.LogInformation($"creating data source [name={conn.Key}] [value={conn.Value}]");
                if (string.IsNullOrEmpty(conn.Value)) {
                    _Logger.LogWarning($"skipping DB with no connection string [name={conn.Key}]");
                    continue;
                }
                string dbName = conn.Key.Split(":")[1];
                NpgsqlDataSource ds = new NpgsqlDataSourceBuilder(conn.Value) {
                    Name = dbName
                }.Build();

                if (_DataSources.TryAdd(dbName, ds) == false) {
                    _Logger.LogError($"failed to add datasource to dict [dbName={dbName}]");
                }
            }
        }

        /// <summary>
        ///     Create a new connection to a database
        /// </summary>
        /// <remarks>
        ///     The following additional properties are set on the connection:
        ///         <br/>
        ///         'Include Error Detail'=true
        ///         <br/>
        ///         ApplicationName='Gex'
        ///         <br/>
        ///         Timezone=UTC
        /// </remarks>
        /// <returns>
        ///     A new <see cref="NpgsqlConnection"/>
        /// </returns>
        public DbConnection Connection(string server = Dbs.MAIN, string? task = null, bool enlist = true) {
            NpgsqlDataSource ds = _DataSources.GetValueOrDefault(server) ??
                throw new Exception($"No connection string for {server} exists. Currently have [{string.Join(", ", _DataSources.Keys)}]. "
                    + $"Set this value in config, or by using 'dotnet user-secrets set ConnectionStrings:{server} {{connection string}}");

            return ds.CreateConnection();
        }

        /// <summary>
        ///     Create a new command, using the connection passed
        /// </summary>
        /// <remarks>
        ///     The resulting <see cref="NpgsqlCommand"/> will have <see cref="DbCommand.CommandType"/> of <see cref="CommandType.Text"/>,
        ///     and <see cref="DbCommand.CommandText"/> of <paramref name="text"/>
        /// </remarks>
        /// <param name="connection">Connection to create the command on</param>
        /// <param name="text">Command text</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     A new <see cref="NpgsqlCommand"/> ready to be used
        /// </returns>
        public async Task<DbCommand> Command(DbConnection connection, string text, CancellationToken cancel = default) {
            if (connection.State == ConnectionState.Closed) {
                await connection.OpenAsync(cancel);
            }

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = text;

            return cmd;
        }

    }

}
