using gex.Models.Db;
using gex.Models.UserStats;
using gex.Services.Db.UserStats;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarUserRepository {

        private readonly ILogger<BarUserRepository> _Logger;
        private readonly BarUserDb _UserDb;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ID = "Gex.User.{0}"; // {0} => user ID

        public BarUserRepository(ILogger<BarUserRepository> logger,
            BarUserDb userDb, IMemoryCache cache) {

            _Logger = logger;

            _UserDb = userDb;
            _Cache = cache;
        }

        /// <summary>
        ///     get a specific <see cref="BarUser"/> by its <see cref="BarUser.UserID"/>
        /// </summary>
        /// <param name="userID">ID of the <see cref="BarUser"/> to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     the <see cref="BarUser"/> with the <see cref="BarUser.UserID"/> of <paramref name="userID"/>,
        ///     or <c>null</c> if it doesnt exist
        /// </returns>
        public async Task<BarUser?> GetByID(long userID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, userID);
            if (_Cache.TryGetValue(cacheKey, out BarUser? user) == false) {
                user = await _UserDb.GetByID(userID, cancel);

                _Cache.Set(cacheKey, user, new MemoryCacheEntryOptions() {
                    SlidingExpiration = (user == null) ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(5)
                });
            }

            return user;
        }

        /// <summary>
        ///     update/insert (upsert) a <see cref="BarUser"/>
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="user"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task Upsert(long userID, BarUser user, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, userID);
            _Cache.Remove(userID);

            return _UserDb.Upsert(userID, user, cancel);
        }

        /// <summary>
        ///     select all <see cref="BarUser"/>s from the DB
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public Task<List<BarUser>> GetAll(CancellationToken cancel) {
            return _UserDb.GetAll(cancel);
        }

        /// <summary>
        ///     search for user by name, and optionally previous names. case-insensitive
        /// </summary>
        /// <param name="name">name to search for</param>
        /// <param name="includePreviousNames">will previous names be searched as well</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a list of <see cref="UserSearchResult"/>s</returns>
        public Task<List<UserSearchResult>> SearchByName(string name, bool includePreviousNames, CancellationToken cancel) {
            return _UserDb.SearchByName(name, includePreviousNames, cancel);
        }

        /// <summary>
        ///     get all names that a user has used
        /// </summary>
        /// <param name="userID">ID of the user to get the previous names of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of <see cref="UserPreviousName"/>s that represent the past names of a user
        /// </returns>
        public Task<List<UserPreviousName>> GetUserNames(long userID, CancellationToken cancel) {
            return _UserDb.GetUserNames(userID, cancel);
        }

    }
}
