using Dapper;
using System.Data.Common;

namespace gex.Common.Code.ExtensionMethods {

    public static class DbConnectionExtensionMethods {

        public static async Task<List<T>> QueryListAsync<T>(this DbConnection conn, string query, CancellationToken cancellationToken) {
            if (conn.State != System.Data.ConnectionState.Open) {
                await conn.OpenAsync(cancellationToken);
            }

            return (await conn.QueryAsync<T>(new CommandDefinition(
                query,
                cancellationToken: cancellationToken
            ))).ToList();
        }

        public static async Task<List<T>> QueryListAsync<T>(this DbConnection conn, string query, object? parms, CancellationToken cancellationToken) {
            if (conn.State != System.Data.ConnectionState.Open) {
                await conn.OpenAsync(cancellationToken);
            }

            return (await conn.QueryAsync<T>(new CommandDefinition(
                query,
                parms,
                cancellationToken: cancellationToken
            ))).ToList();
        }

        public static async Task<T?> QuerySingleAsync<T>(this DbConnection conn, string query, object? parms, CancellationToken cancellationToken) {
            if (conn.State != System.Data.ConnectionState.Open) {
                await conn.OpenAsync(cancellationToken);
            }

            return await conn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(
                query,
                parms,
                cancellationToken: cancellationToken
            ));
        }

    }
}
