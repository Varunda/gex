using gex.Code.ExtensionMethods;
using gex.Models.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Account {

    public class AppAccountGroupMembershipDb {

        private readonly ILogger<AppAccountGroupMembershipDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public AppAccountGroupMembershipDb(ILogger<AppAccountGroupMembershipDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;

            _DbHelper = dbHelper;
        }

        public async Task<AppAccountGroupMembership?> GetByID(long memberID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
			return await conn.QuerySingleAsync<AppAccountGroupMembership>(
				"SELECT * FROM app_account_group_membership WHERE id = @ID",
				new { ID = memberID },
				cancel
			);
        }

        /// <summary>
        ///     get the group memberships a user is a part of
        /// </summary>
        /// <param name="accountID">ID of the account to get the memberships of</param>
		/// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     
        /// </returns>
        public async Task<List<AppAccountGroupMembership>> GetByAccountID(long accountID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
			return await conn.QueryListAsync<AppAccountGroupMembership>(
				"SELECT * FROM app_account_group_membership WHERE account_id = @ID",
				new { ID = accountID },
				cancel
			);
        }

        /// <summary>
        ///     get the group memberships a user is a part of
        /// </summary>
        /// <param name="groupID">ID of the group to get the memberships of</param>
		/// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     
        /// </returns>
        public async Task<List<AppAccountGroupMembership>> GetByGroupID(long groupID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
			return await conn.QueryListAsync<AppAccountGroupMembership>(
				"SELECT * FROM app_account_group_membership WHERE group_id = @ID",
				new { ID = groupID },
				cancel
			);
        }

        public async Task<long> Insert(AppAccountGroupMembership membership, CancellationToken cancel) {
            if (membership.AccountID == 0) {
                throw new ArgumentException($"account id cannot be 0");
            }
            if (membership.GroupID == 0) {
                throw new ArgumentException($"group id cannot be 0");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_account_group_membership (
                    account_id, group_id, timestamp, granted_by_account_id
                ) VALUES (
                    @AccountID, @GroupID, @Timestamp, @GrantedBy
                ) RETURNING id;
            ", cancel);

            cmd.AddParameter("AccountID", membership.AccountID);
            cmd.AddParameter("GroupID", membership.GroupID);
            cmd.AddParameter("Timestamp", membership.Timestamp);
            cmd.AddParameter("GrantedBy", membership.GrantedByAccountID);
            await cmd.PrepareAsync(cancel);

            long id = await cmd.ExecuteInt64(cancel);
            await conn.CloseAsync();

            return id;
        }

        public async Task Delete(AppAccountGroupMembership membership, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM 
                    app_account_group_membership
                WHERE
                    group_id = @GroupID
                    AND account_id = @AccountID
            ", cancel);

            cmd.AddParameter("AccountID", membership.AccountID);
            cmd.AddParameter("GroupID", membership.GroupID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
