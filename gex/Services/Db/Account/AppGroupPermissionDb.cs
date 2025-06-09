using gex.Code.ExtensionMethods;
using gex.Models.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Account {

    public class AppGroupPermissionDb {

        private readonly ILogger<AppGroupPermissionDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public AppGroupPermissionDb(ILogger<AppGroupPermissionDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     Get a single group permission by its ID
        /// </summary>
        /// <param name="ID">ID of the specific permission to get</param>
        /// <param name="cancel">cancellation token</param>
        public async Task<AppGroupPermission?> GetByID(long ID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            return await conn.QuerySingleAsync<AppGroupPermission>(
                "SELECT * FROM app_group_permission WHERE id = @ID",
                new { ID = ID },
                cancel
            );
        }

        /// <summary>
        ///     Get the account permissions of a group
        /// </summary>
        /// <param name="groupID">ID of the group</param>
        /// <param name="cancel">cancellation token</param>
        public async Task<List<AppGroupPermission>> GetByGroupID(long groupID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            return await conn.QueryListAsync<AppGroupPermission>(
                "SELECT * FROM app_group_permission WHERE group_id = @GroupID",
                new { GroupID = groupID },
                cancel
            );
        }

        /// <summary>
        ///     Insert a new <see cref="AppGroupPermission"/>
        /// </summary>
        /// <param name="perm">Parameters used to insert</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>The ID the row was given in the table</returns>
        /// <exception cref="ArgumentException">If one of the fields in <paramref name="perm"/> was invalid</exception>
        public async Task<ulong> Insert(AppGroupPermission perm, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(perm.Permission)) {
                throw new ArgumentException($"Passed permission has a {nameof(AppGroupPermission.Permission)} that is null or whitespace");
            }
            if (perm.GrantedByID <= 0) {
                throw new ArgumentException($"Passed permission has a {nameof(AppGroupPermission.GrantedByID)} that is 0 or lower");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_group_permission (
                    group_id, permission, timestamp, granted_by_id
                ) VALUES (
                    @GroupID, @Permission, @Timestamp, @GrantedByID 
                ) RETURNING id;
            ", cancel);

            cmd.AddParameter("GroupID", perm.GroupID);
            cmd.AddParameter("Permission", perm.Permission);
            cmd.AddParameter("Timestamp", DateTime.UtcNow);
            cmd.AddParameter("GrantedByID", perm.GrantedByID);
            await cmd.PrepareAsync(cancel);

            ulong id = await cmd.ExecuteUInt64(cancel);

            return id;
        }

        /// <summary>
        ///     Delete a specific <see cref="AppGroupPermission"/> by its ID
        /// </summary>
        /// <param name="ID">ID of the permission to delete</param>
        /// <param name="cancel">cancellation token</param>
        public async Task DeleteByID(long ID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE 
                    FROM app_group_permission
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
