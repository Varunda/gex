using gex.Code.ExtensionMethods;
using gex.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Account {

    public class AppAccountDbStore {

        private readonly ILogger<AppAccountDbStore> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<AppAccount> _Reader;

        public AppAccountDbStore(ILogger<AppAccountDbStore> logger,
            IDbHelper helper, IDataReader<AppAccount> reader) {

            _Logger = logger;
            _DbHelper = helper;
            _Reader = reader;
        }

        /// <summary>
        ///     Get a specific <see cref="AppAccount"/> by its ID
        /// </summary>
        /// <param name="ID">ID of the account to get</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>
        ///     The <see cref="AppAccount"/> with <see cref="AppAccount.ID"/> of <paramref name="ID"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<AppAccount?> GetByID(long ID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_account
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);

            return await cmd.ExecuteReadSingle(_Reader, cancel);
        }

        /// <summary>
        ///     Get all <see cref="AppAccount"/>s
        /// </summary>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>
        ///     A list of all <see cref="AppAccount"/>s, including ones that are deactivated
        /// </returns>
        public async Task<List<AppAccount>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_account;
            ");

            return await cmd.ExecuteReadList(_Reader, cancel);
        }

        /// <summary>
        ///     Get a <see cref="AppAccount"/> by <see cref="AppAccount.DiscordID"/>
        /// </summary>
        /// <param name="discordID">ID of the discord account to get the gex account of</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>
        ///     The <see cref="AppAccount"/> with <see cref="AppAccount.DiscordID"/> of <paramref name="discordID"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<AppAccount?> GetByDiscordID(ulong discordID, CancellationToken cancel = default) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_account
                    WHERE discord_id = @DiscordID;
            ");

            cmd.AddParameter("DiscordID", discordID);

            return await cmd.ExecuteReadSingle(_Reader, cancel);
        }

        /// <summary>
        ///     Insert a new <see cref="AppAccount"/>
        /// </summary>
        /// <param name="param">Parameters used to insert the new account</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>
        ///     The <see cref="AppAccount.ID"/> of the row that was newly inserted in the DB
        /// </returns>
        public async Task<long> Insert(AppAccount param, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_account (
                    name, discord_id, timestamp 
                ) VALUES (
                    @Name, @DiscordID, NOW() AT TIME ZONE 'utc'
                ) RETURNING id;
            ");

            cmd.AddParameter("Name", param.Name);
            cmd.AddParameter("DiscordID", param.DiscordID);

            return await cmd.ExecuteInt64(cancel);
        }

        /// <summary>
        ///     Delete an account, marking it as deactive
        /// </summary>
        /// <param name="accountID">ID of the account to delete</param>
        /// <param name="deletedByID">ID of the account that is performing the delete</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>
        ///     When the operation has completed
        /// </returns>
        public async Task Delete(long accountID, long deletedByID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE app_account
                    SET deleted_on = NOW() AT TIME ZONE 'utc',
                        deleted_by = @DeletedByID
                    WHERE id = @ID;
            ");

            cmd.AddParameter("DeletedByID", deletedByID);
            cmd.AddParameter("ID", accountID);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
