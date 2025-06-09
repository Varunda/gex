using gex.Models;
using gex.Models.Internal;
using gex.Services.Db.Account;
using gex.Services.Repositories.Account;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class AppPermissionRepository {

        private readonly ILogger<AppPermissionRepository> _Logger;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY = "App.Permission.{0}"; // {0} => groupd ID

        private readonly AppGroupPermissionDb _PermissionDb;
        private readonly AppAccountGroupMembershipRepository _MembershipRepository;

        public AppPermissionRepository(ILogger<AppPermissionRepository> logger, IMemoryCache cache,
            AppGroupPermissionDb permissionDb, AppAccountGroupMembershipRepository membershipRepository) {

            _Logger = logger;
            _Cache = cache;

            _PermissionDb = permissionDb;
            _MembershipRepository = membershipRepository;
        }

        /// <summary>
        ///     Get a specific <see cref="AppGroupPermission"/> by its ID, or null if it doens't exist
        /// </summary>
        public Task<AppGroupPermission?> GetByID(long ID, CancellationToken cancel) {
            return _PermissionDb.GetByID(ID, cancel);
        }

        public async Task<List<AppGroupPermission>> GetByAccountID(long accountID, CancellationToken cancel) {
            _Logger.LogDebug($"getting permission of account [accountID={accountID}]");
            Dictionary<string, AppGroupPermission> perms = new();

            List<AppAccountGroupMembership> groups = await _MembershipRepository.GetByAccountID(accountID, cancel);

            foreach (AppAccountGroupMembership member in groups) {
                List<AppGroupPermission> groupPerms = await GetByGroupID(member.GroupID, cancel);

                foreach (AppGroupPermission p in groupPerms) {
                    if (perms.ContainsKey(p.Permission) == false) {
                        perms.Add(p.Permission, p);
                    }
                }
            }

            return perms.Values.ToList();
        }

        /// <summary>
        ///     Get the <see cref="AppGroupPermission"/>s of a group
        /// </summary>
        /// <param name="groupID">ID of the group</param>
        /// <param name="cancel">cancellation token</param>
        public async Task<List<AppGroupPermission>> GetByGroupID(long groupID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, groupID);

            if (_Cache.TryGetValue(cacheKey, out List<AppGroupPermission>? perms) == false || perms == null) {
                perms = await _PermissionDb.GetByGroupID(groupID, cancel);

                _Cache.Set(cacheKey, perms, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }

            return perms;
        }

        /// <summary>
        ///     Insert a new <see cref="AppGroupPermission"/>, returning the ID it has after being inserted
        /// </summary>
        /// <param name="perm">permission to insert</param>
        /// <param name="cancel">cancellation token</param>
        public Task<ulong> Insert(AppGroupPermission perm, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, perm.GroupID);
            _Cache.Remove(cacheKey);

            return _PermissionDb.Insert(perm, cancel);
        }

        /// <summary>
        ///     Delete a <see cref="AppGroupPermission"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task DeleteByID(long ID, CancellationToken cancel) {
            AppGroupPermission? perm = await GetByID(ID, cancel);
            if (perm != null) {
                string cacheKey = string.Format(CACHE_KEY, perm.GroupID);
                _Cache.Remove(cacheKey);
            }

            await _PermissionDb.DeleteByID(ID, cancel);
        }

    }

    /// <summary>
    ///     Useful extensions method for a <see cref="AppPermissionRepository"/>
    /// </summary>
    public static class AppGroupPermissionRepositoryExtensionMethods {

        /// <summary>
        ///     Get the <see cref="AppGroupPermission"/> for a <see cref="AppAccount"/> based on a list of permissions
        /// </summary>
        /// <param name="repo">Extension instance</param>
        /// <param name="group">group to get the permission of</param>
        /// <param name="cancel">cancellation token</param>
        /// <param name="permissions">Permissions to return</param>
        /// <returns>
        ///     The first <see cref="AppGroupPermission"/> that the account <paramref name="group"/> has that matches
        ///     one of the permission keys in <paramref name="permissions"/>.
        ///     Or <c>null</c> if the user does not have any of those permissions
        /// </returns>
        public static Task<AppGroupPermission?> GetPermissionByGroup(this AppPermissionRepository repo, AppGroup group,
            CancellationToken cancel, params string[] permissions) {
            return repo.GetPermissionByGroupID(group.ID, cancel, permissions);
        }

        /// <summary>
        ///     Get the <see cref="AppGroupPermission"/> for a <see cref="AppAccount"/> based on a list of permissions
        /// </summary>
        /// <param name="repo">Extension instance</param>
        /// <param name="groupID">ID of the group to get the permission of</param>
        /// <param name="cancel">cancellation token</param>
        /// <param name="permissions">Permissions to return</param>
        /// <returns>
        ///     The first <see cref="AppGroupPermission"/> that the account with <see cref="AppAccount.ID"/>
        ///     of <paramref name="groupID"/> has that matches one of the permission keys in <paramref name="permissions"/>.
        ///     Or <c>null</c> if the user does not have any of those permissions
        /// </returns>
        public static async Task<AppGroupPermission?> GetPermissionByGroupID(this AppPermissionRepository repo,
            long groupID, CancellationToken cancel, params string[] permissions) {

            List<AppGroupPermission> perms = await repo.GetByGroupID(groupID, cancel);

            foreach (AppGroupPermission perm in perms) {
                foreach (string testPerm in permissions) {
                    if (perm.Permission.ToLower() == testPerm.ToLower()) {
                        return perm;
                    }
                }
            }

            return null;
        }

    }
}
