using gex.Code;
using gex.Models;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Services;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/match-pool")]
    public class MatchPoolApiController : ApiControllerBase {

        private readonly ILogger<MatchPoolApiController> _Logger;
        private readonly ICurrentAccount _CurrentUser;
        private readonly AppPermissionRepository _PermissionRepository;

        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;
        private readonly BarMatchRepository _MatchRepository;

        public MatchPoolApiController(ILogger<MatchPoolApiController> logger,
            MatchPoolRepository matchPoolRepository, MatchPoolEntryDb matchPoolEntryDb,
            ICurrentAccount currentUser, BarMatchRepository matchRepository,
            AppPermissionRepository permissionRepository) {

            _Logger = logger;
            _MatchPoolRepository = matchPoolRepository;
            _MatchPoolEntryDb = matchPoolEntryDb;
            _CurrentUser = currentUser;
            _MatchRepository = matchRepository;
            _PermissionRepository = permissionRepository;
        }

        /// <summary>
        ///     get all <see cref="MatchPool"/>s that are not hidden to the user making the request. see remarks for more
        /// </summary>
        /// <remarks>
        ///     a <see cref="MatchPool"/> is included in the response if any of the following conditions are met:
        ///     <ul>
        ///         <li>the <see cref="MatchPool"/> has <see cref="MatchPool.Hidden"/> false</li>
        ///         <li>the user making the request has the <see cref="AppPermission.GEX_DEV"/> permission</li>
        ///         <li>the user making the request is the user that made the <see cref="MatchPool"/> (and the pool is hidden)</li>
        ///     </ul>
        /// </remarks>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of all <see cref="MatchPool"/>s
        /// </response>
        [HttpGet]
        public async Task<ApiResponse<List<MatchPool>>> GetPools(CancellationToken cancel = default) {

            AppAccount? currentUser = await _CurrentUser.Get(cancel);
            bool includeHidden = await _PermissionRepository.HasPermission(currentUser?.ID ?? -1, [AppPermission.GEX_DEV], cancel);

            List<MatchPool> pools = await _MatchPoolRepository.GetAll(cancel);

            if (includeHidden == false) {
                pools = pools.Where(iter => {
                    return iter.Hidden == false || iter.CreatedByID == currentUser?.ID;
                }).ToList();
            }

            return ApiOk(pools);
        }

        /// <summary>
        ///     get a specific <see cref="MatchPool"/>
        /// </summary>
        /// <param name="poolID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("{poolID}")]
        public async Task<ApiResponse<MatchPool>> GetByID(long poolID, CancellationToken cancel = default) {
            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return ApiNoContent<MatchPool>();
            }

            return ApiOk(pool);
        }

        /// <summary>
        ///     create a new <see cref="MatchPool"/>
        /// </summary>
        /// <param name="name">name of the match pool to create</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the newly created <see cref="MatchPool"/>
        /// </response>
        [HttpPost]
        [PermissionNeeded(AppPermission.GEX_MATCH_POOL_CREATE)]
        public async Task<ApiResponse<MatchPool>> Create([FromQuery] string name, CancellationToken cancel = default) {
            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new System.Exception($"expected current user to exist here");

            MatchPool pool = new() {
                Name = name,
                CreatedByID = currentUser.ID,
            };

            long poolID = await _MatchPoolRepository.Create(pool, cancel);
            pool.ID = poolID;

            _Logger.LogInformation($"created new match pool [id={pool.ID}] [name={pool.Name}] [created by={currentUser.Name}/{currentUser.ID}]");

            return ApiOk(pool);
        }

        /// <summary>
        ///     update a <see cref="MatchPool"/> with new <see cref="MatchPool.Name"/> and <see cref="MatchPool.Hidden"/> status
        /// </summary>
        /// <param name="poolID">ID of the <see cref="MatchPool"/> to be updated</param>
        /// <param name="name">new name of the <see cref="MatchPool"/></param>
        /// <param name="hidden">new value of <see cref="MatchPool.Hidden"/></param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the updated <see cref="MatchPool"/>
        /// </response>
        /// <response code="400">
        ///     <paramref name="name"/> was empty or only contained whitespace. if no change is wanted, provide the original name
        /// </response>
        /// <response code="404">
        ///     no <see cref="MatchPool"/> with <see cref="MatchPool.ID"/> of <paramref name="poolID"/> exists
        /// </response>
        [HttpPost("{poolID}")]
        [Authorize]
        public async Task<ApiResponse<MatchPool>> Update(long poolID, [FromQuery] string name, [FromQuery] bool hidden, CancellationToken cancel) {
            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new System.Exception($"expected current user to exist here");

            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return ApiNotFound<MatchPool>($"{nameof(MatchPool)} {poolID}");
            }

            if (await _IsPoolCreatorOrDev(poolID, cancel) == false) {
                return ApiForbidden<MatchPool>($"user is not the creator of this match pool (or a dev)");
            }

            if (string.IsNullOrWhiteSpace(name)) {
                return ApiBadRequest<MatchPool>($"{nameof(name)} cannot be empty or contain only whitespace");
            }

            pool.Name = name;
            pool.Hidden = hidden;

            _Logger.LogDebug($"updating match pool [poolID={poolID}] [name={pool.Name}] [hidden={pool.Hidden}]");
            await _MatchPoolRepository.Update(poolID, pool, cancel);

            return ApiOk(pool);
        }

        /// <summary>
        ///     get the list of <see cref="MatchPoolEntry"/>s for a <see cref="MatchPool"/>
        /// </summary>
        /// <param name="poolID">ID of the match pool</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="MatchPoolEntry"/>s with 
        ///     <see cref="MatchPoolEntry.PoolID"/> of <paramref name="poolID"/>
        /// </response>
        /// <response code="404">
        ///     no <see cref="MatchPool"/> with <see cref="MatchPool.ID"/> of <paramref name="poolID"/> exists
        /// </response>
        [HttpGet("{poolID}/entries")]
        public async Task<ApiResponse<List<MatchPoolEntry>>> GetEntriesByPoolID(long poolID, CancellationToken cancel = default) {
            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return ApiNotFound<List<MatchPoolEntry>>($"{nameof(MatchPool)} {poolID}");
            }

            List<MatchPoolEntry> entries = await _MatchPoolEntryDb.GetByPoolID(poolID, cancel);
            return ApiOk(entries);
        }

        /// <summary>
        ///     add a match to a match pool
        /// </summary>
        /// <param name="poolID"></param>
        /// <param name="matchID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        [HttpPost("{poolID}/{matchID}")]
        [Authorize]
        public async Task<ApiResponse> AddMatchToPool(long poolID, string matchID, CancellationToken cancel = default) {

            if (await _IsPoolCreatorOrDev(poolID, cancel) == false) {
                return ApiForbidden($"user is not the creator of this match pool (or a dev)");
            }

            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new System.Exception($"expected current user to exist here");

            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return ApiNotFound($"{nameof(MatchPool)} {poolID}");
            }

            BarMatch? match = await _MatchRepository.GetByID(matchID, cancel);
            if (match == null) {
                return ApiNotFound($"{nameof(BarMatch)} {matchID}");
            }

            List<MatchPoolEntry> entries = await _MatchPoolEntryDb.GetByPoolID(poolID, cancel);
            if (entries.FirstOrDefault(iter => iter.MatchID == matchID) != null) {
                return ApiBadRequest($"{nameof(BarMatch)} {matchID} is already in {nameof(MatchPool)} {poolID}");
            }

            MatchPoolEntry entry = new() {
                PoolID = poolID,
                MatchID = matchID,
                AddedByID = currentUser.ID
            };

            await _MatchPoolEntryDb.Insert(entry, cancel);

            _Logger.LogInformation($"match added to match pool [poolID={poolID}] [matchID={matchID}]");

            return ApiOk();
        }

        /// <summary>
        ///     remove a <see cref="MatchPoolEntry"/> from a match pool
        /// </summary>
        /// <param name="poolID"></param>
        /// <param name="matchID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        [HttpDelete("{poolID}/{matchID}")]
        [Authorize]
        public async Task<ApiResponse> RemoveMatchFromPool(long poolID, string matchID, CancellationToken cancel = default) {

            if (await _IsPoolCreatorOrDev(poolID, cancel) == false) {
                return ApiForbidden($"user is not the creator of this match pool (or a dev)");
            }

            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new System.Exception($"expected current user to exist here");

            MatchPoolEntry entry = new() {
                PoolID = poolID,
                MatchID = matchID,
                AddedByID = currentUser.ID
            };

            await _MatchPoolEntryDb.Remove(entry, cancel);

            _Logger.LogInformation($"match removed from match pool [poolID={poolID}] [matchID={matchID}]");

            return ApiOk();
        }

        /// <summary>
        ///     check if the user making the request has the Gex.Dev permissions or is the 
        ///     creator of the pool
        /// </summary>
        /// <param name="poolID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private async Task<bool> _IsPoolCreatorOrDev(long poolID, CancellationToken cancel) {
            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new System.Exception($"expected current user to exist here");

            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return false;
            }

            bool isDev = await _PermissionRepository.HasPermission(currentUser.ID, [AppPermission.GEX_DEV], cancel);

            return isDev == true || pool.CreatedByID == currentUser.ID;
        }

    }
}
