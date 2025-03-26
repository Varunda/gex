using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gex.Models.Db;

namespace gex.Services.Db.Implementations {

    public class DbHelper : IDbHelper {

        private readonly ILogger<DbHelper> _Logger;
        private readonly IConfiguration _Configuration;

        public DbHelper(ILogger<DbHelper> logger, IConfiguration config) {
            _Logger = logger;
            _Configuration = config;

            IConfigurationSection allStrings = _Configuration.GetSection("ConnectionStrings");
            string? connStr = allStrings[Dbs.MAIN];

            _Logger.LogInformation($"db helper setup [conn str={connStr}]");
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
        public NpgsqlConnection Connection(string server = Dbs.MAIN, string? task = null, bool enlist = true) {
            IConfigurationSection allStrings = _Configuration.GetSection("ConnectionStrings");
            string? connStr = allStrings[server];

            if (string.IsNullOrEmpty(connStr)) {
                throw new Exception($"No connection string for {server} exists. Currently have [{string.Join(", ", allStrings.GetChildren().ToList().Select(iter => iter.Path))}]. "
                    + $"Set this value in config, or by using 'dotnet user-secrets set ConnectionStrings:{server} {{connection string}}");
            }

            if (enlist == false) {
                connStr += ";Enlist=false";
            }

            NpgsqlConnection conn = new(connStr);

            return conn;
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
        public async Task<NpgsqlCommand> Command(NpgsqlConnection connection, string text, CancellationToken cancel = default) {
            await connection.OpenAsync(cancel);

            NpgsqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = text;

            return cmd;
        }

    }

}
