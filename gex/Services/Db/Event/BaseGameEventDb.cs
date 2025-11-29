using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Event {

    public abstract class BaseGameEventDb<T> where T : GameEvent {

        private readonly ILogger _Logger;
        private readonly IDbHelper _DbHelper;

        private readonly string _TableName;
        private readonly string _ActionName;

        public BaseGameEventDb(string tableName, string actionName,
            ILoggerFactory loggerFactory, IDbHelper dbHelper) {

            _Logger = loggerFactory.CreateLogger($"gex.Services.Db.BaseGameEventDb<{tableName}>");
            _DbHelper = dbHelper;

            _TableName = tableName;
            _ActionName = actionName;
        }

        /// <summary>
        ///     insert a new <typeparamref name="T"/> into the DB
        /// </summary>
        /// <param name="ev">event to insert</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for when the async operation is complete</returns>
        /// <exception cref="System.Exception">
        ///     if the <see cref="GameEvent.GameID"/> of <paramref name="ev"/> is empty or null
        /// </exception>
        public async Task Insert(T ev, CancellationToken cancel = default) {
            if (string.IsNullOrEmpty(ev.GameID)) {
                throw new System.Exception($"missing GameID for {nameof(T)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"", cancel);

            SetupInsert(ev, cmd);

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task InsertMany(IEnumerable<T> events, CancellationToken cancel = default) {
            foreach (T ev in events) {
                await Insert(ev, cancel);
            }
        }

        /// <summary>
        ///     setup the insert operation for an event. it is expected this method will 
        ///     set <see cref="NpgsqlCommand.CommandText"/>, and add all parameters
        ///     (usually thru <see cref="NpgsqlCommandExtensionMethod.AddParameter(NpgsqlCommand, string, object?)"/>).
        ///     there is no need to <see cref="DbCommand.PrepareAsync(CancellationToken)"/>,
        ///     as that is done in <see cref="Insert(T, CancellationToken)"/>
        /// </summary>
        /// <param name="ev">event that contains the parameters to use</param>
        /// <param name="cmd">command</param>
        protected abstract void SetupInsert(T ev, NpgsqlCommand cmd);

        /// <summary>
        ///     get the <typeparamref name="T"/> for a specific game ID
        /// </summary>
        /// <param name="gameID">ID of the game to get the values of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of <typeparamref name="T"/> with <see cref="GameEvent.GameID"/> of <paramref name="gameID"/>
        /// </returns>
        public async Task<List<T>> GetByGameID(string gameID, CancellationToken cancel = default) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            return await conn.QueryListAsync<T>(
                $"SELECT '{_ActionName}' \"Action\", * from {_TableName} WHERE game_id = @GameID ORDER BY frame ASC",
                new { GameID = gameID },
                cancellationToken: cancel
            );
        }

        /// <summary>
        ///     delete the events of <typeparamref name="T"/> from the DB for a specific game ID
        /// </summary>
        /// <param name="gameID">ID of the game to delete the events of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for when the async operation is complete</returns>
        public async Task DeleteByGameID(string gameID, CancellationToken cancel = default) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                DELETE FROM {_TableName}
                    WHERE game_id = @GameID;
            ", cancel);

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
