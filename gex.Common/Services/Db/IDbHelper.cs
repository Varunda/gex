using gex.Common.Code.ExtensionMethods;
using System.Data.Common;

namespace gex.Common.Services.Db {

    public interface IDbHelper {

        /// <summary>
        /// Create a new connection to the database given in the db options
        /// </summary>
        /// <param name="server">Name of the server. Currently setup for 'events' and 'character'</param>
        /// <param name="task">Optional name to use about the application, defaults to 'gex'</param>
        /// <param name="enlist">Will this connection enlist to the TransactionScope?</param>
        /// <returns>A new connection to use</returns>
        DbConnection Connection(string server = Dbs.MAIN, string? task = null, bool enlist = true);

        /// <summary>
        /// Create a new command using the connection passed
        /// </summary>
        /// <param name="connection">Connection the command will be executed on</param>
        /// <param name="text">Text of the command</param>
        /// <param name="cancel">cancellation token</param>
        Task<DbCommand> Command(DbConnection connection, string text, CancellationToken cancel = default);

    }

    /// <summary>
    ///     Names of connection strings
    /// </summary>
    public static class Dbs {

        /// <summary>
        ///     main DB
        /// </summary>
        public const string MAIN = "gex";

        /// <summary>
        ///     event DB
        /// </summary>
        public const string EVENT = "event";

    }

    public static class IDbHelperExtensions {

        public static async Task<bool> HasColumn(this IDbHelper instance, string tableName, string column) {
            using DbConnection conn = instance.Connection();
            using DbCommand cmd = await instance.Command(conn, @"
                SELECT *
                FROM information_schema.columns
                WHERE table_name = @TableName AND column_name = @Column;
            ");

            cmd.AddParameter("TableName", tableName);
            cmd.AddParameter("Column", column);

            using DbDataReader reader = await cmd.ExecuteReaderAsync();

            bool hasIndex = await reader.ReadAsync();

            return hasIndex;
        }

    }

}
