using gex.Models.Db;
using gex.Models.Internal;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class MatchPoolRepository {

        private readonly ILogger<MatchPoolRepository> _Logger;
        private readonly MatchPoolDb _MatchPoolDb;

        private readonly AppPermissionRepository _PermissionRepository;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_ALL = "Gex.MatchPool.All";

        public MatchPoolRepository(ILogger<MatchPoolRepository> logger,
            MatchPoolDb matchPoolDb, IMemoryCache cache,
            AppPermissionRepository permissionRepository) {

            _Logger = logger;
            _MatchPoolDb = matchPoolDb;
            _Cache = cache;
            _PermissionRepository = permissionRepository;
        }

        public async Task<List<MatchPool>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out List<MatchPool>? pools) == false || pools == null) {
                pools = await _MatchPoolDb.GetAll(cancel);

                _Cache.Set(CACHE_KEY_ALL, pools, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return pools;
        }

        public async Task<MatchPool?> GetByID(long poolID, CancellationToken cancel) {
            return await _MatchPoolDb.GetByID(poolID, cancel);
        }

        public async Task<long> Create(MatchPool pool, CancellationToken cancel) {
            _Cache.Remove(CACHE_KEY_ALL);
            return await _MatchPoolDb.Create(pool, cancel);
        }

        public async Task Update(long poolID, MatchPool pool, CancellationToken cancel) {
            _Cache.Remove(CACHE_KEY_ALL);
            await _MatchPoolDb.Update(poolID, pool, cancel);
        }

        /// <summary>
        ///     check if a user can view a <see cref="MatchPool"/>, or any user if <paramref name="appUserID"/> is null
        /// </summary>
        /// <param name="poolID">ID of the <see cref="MatchPool"/> to check permission to view</param>
        /// <param name="appUserID">ID of the user to check the permission of, or <c>null</c> if any user</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<bool> CanView(long poolID, long? appUserID, CancellationToken cancel) {
            MatchPool? pool = await GetByID(poolID, cancel);
            if (pool == null) {
                return false;
            }

            if (pool.HideUntil == null) {
                return true;
            }

            bool isDev = appUserID != null && await _PermissionRepository.HasPermission(appUserID.Value, [AppPermission.GEX_DEV], cancel);
            _Logger.LogTrace($"checking if user can view match pool [poolID={poolID}] [isDev={isDev}] [appUserID={appUserID}] [hideUntil={pool.HideUntil.Value:u}]");
            return isDev == true || pool.CreatedByID == appUserID || DateTime.UtcNow >= pool.HideUntil.Value;
        }

    }
}
