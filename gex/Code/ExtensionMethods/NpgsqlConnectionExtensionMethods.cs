using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.ExtensionMethods {

    public static class NpgsqlConnectionExtensionMethods {

        public static async Task<List<T>> QueryListAsync<T>(this NpgsqlConnection conn, string query, CancellationToken cancellationToken) {
            return (await conn.QueryAsync<T>(new CommandDefinition(
                query,
                cancellationToken: cancellationToken
            ))).ToList();
        }

        public static async Task<List<T>> QueryListAsync<T>(this NpgsqlConnection conn, string query, object? parms, CancellationToken cancellationToken) {
            return (await conn.QueryAsync<T>(new CommandDefinition(
                query,
                parms,
                cancellationToken: cancellationToken
            ))).ToList();
        }

        public static async Task<T?> QuerySingleAsync<T>(this NpgsqlConnection conn, string query, object? parms, CancellationToken cancellationToken) {
            return await conn.QueryFirstAsync<T>(new CommandDefinition(
                "SELECT * FROM app_group WHERE id = @ID",
                parms,
                cancellationToken: cancellationToken
            ));
        }

    }
}
