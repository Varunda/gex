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
using System;
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
        ///         <li>the <see cref="MatchPool"/> has <see cref="MatchPool.Unlisted"/> false</li>
        ///         <li>the user making the request has the <see cref="AppPermission.GEX_DEV"/> permission</li>
        ///         <li>the user making the request is the user that made the <see cref="MatchPool"/> (and the pool is hidden)</li>
        ///     </ul>
        /// </remarks>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of all <see cref="MatchPool"/>s the user making the request can see.
        ///     if the user is not logged in, it will be a list of match pools that anyone can see
        /// </response>
        [HttpGet]
        public async Task<ApiResponse<List<MatchPool>>> GetPools(CancellationToken cancel = default) {

            AppAccount? currentUser = await _CurrentUser.Get(cancel);
            bool includeHidden = currentUser != null && await _PermissionRepository.HasPermission(currentUser.ID, [AppPermission.GEX_DEV], cancel);

            List<MatchPool> pools = await _MatchPoolRepository.GetAll(cancel);

            if (includeHidden == false) {
                pools = pools.Where(iter => {
                    // creators of unlisted pools can view them
                    if (iter.Unlisted == true && iter.CreatedByID != currentUser?.ID) {
                        return false;
                    }

                    // creators of pools hidden until a certain time can view them
                    if (iter.HideUntil != null && iter.CreatedByID != currentUser?.ID && DateTime.UtcNow < iter.HideUntil) {
                        return false;
                    }

                    return true;
                }).ToList();
            }

            return ApiOk(pools);
        }

        /// <summary>
        ///     get a specific <see cref="MatchPool"/>. any user can view an unlisted match pool,
        ///     but a hidden match pool can only be seen by its creator (or dev perm) until the
        ///     value in <see cref="MatchPool.HideUntil"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="MatchPool"/> to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the <see cref="MatchPool"/> with <see cref="MatchPool.ID"/>
        ///     of <paramref name="poolID"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="MatchPool"/> with <see cref="MatchPool.ID"/> of <paramref name="poolID"/> exists
        /// </response>
        /// <response code="403">
        ///     the user making the request lacks permission to view the <see cref="MatchPool"/>.
        ///     this happens when the match pool has a <see cref="MatchPool.HideUntil"/> in the future,
        ///     and the user is not the creator of the match pool (or a dev)
        /// </response>
        [HttpGet("{poolID}")]
        public async Task<ApiResponse<MatchPool>> GetByID(long poolID, CancellationToken cancel = default) {
            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return ApiNoContent<MatchPool>();
            }

            if (await _CanViewPool(poolID, cancel) == false) {
                return ApiForbidden<MatchPool>($"no permission to view this pool (it is hidden)");
            }

            return ApiOk(pool);
        }

        /// <summary>
        ///     create a new <see cref="MatchPool"/>
        /// </summary>
        /// <param name="name">name of the match pool to create</param>
        /// <param name="unlisted">if the match pool will be unlisted when created</param>
        /// <param name="hideUntil">if the match pool will be hidden until a specific time. can be at most 90 days away</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the newly created <see cref="MatchPool"/>
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li><paramref name="hideUntil"/> was provided, and is in the past</li>
        ///         <li><paramref name="hideUntil"/> was provided, and is more than 90 days in the future</li>
        ///         <li><paramref name="name"/> was blank</li>
        ///     </ul>
        /// </response>
        [HttpPost]
        [PermissionNeeded(AppPermission.GEX_MATCH_POOL_CREATE)]
        public async Task<ApiResponse<MatchPool>> Create([FromQuery] string name,
            [FromQuery] bool unlisted = false,
            [FromQuery] DateTime? hideUntil = null,
            CancellationToken cancel = default
        ) {
            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new Exception($"expected current user to exist here");

            if (hideUntil != null && hideUntil < DateTime.UtcNow) {
                return ApiBadRequest<MatchPool>($"{nameof(hideUntil)} ({hideUntil:u}) cannot be in the past when creating a {nameof(MatchPool)}");
            }
            if (hideUntil != null && (hideUntil - DateTime.UtcNow) > TimeSpan.FromDays(90)) {
                return ApiBadRequest<MatchPool>($"{nameof(hideUntil)} can only be at most 90 days in the future");
            }
            if (string.IsNullOrWhiteSpace(name) == true) {
                return ApiBadRequest<MatchPool>($"{nameof(name)} cannot be blank");
            }

            MatchPool pool = new() {
                Name = name,
                CreatedByID = currentUser.ID,
                Unlisted = unlisted,
                HideUntil = hideUntil
            };

            long poolID = await _MatchPoolRepository.Create(pool, cancel);
            pool.ID = poolID;

            _Logger.LogInformation($"created new match pool [id={pool.ID}] [name={pool.Name}] [created by={currentUser.Name}/{currentUser.ID}]");

            return ApiOk(pool);
        }

        /// <summary>
        ///     update a <see cref="MatchPool"/> with new <see cref="MatchPool.Name"/> and <see cref="MatchPool.Unlisted"/> status
        /// </summary>
        /// <param name="poolID">ID of the <see cref="MatchPool"/> to be updated</param>
        /// <param name="name">new name of the <see cref="MatchPool"/></param>
        /// <param name="unlisted">new value of <see cref="MatchPool.Unlisted"/></param>
        /// <param name="hideUntil">new value of <see cref="MatchPool.HideUntil"/>. can be at most 90 days in the future</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the updated <see cref="MatchPool"/>
        /// </response>
        /// <response code="400">
        ///     one of the following validations failed:
        ///     <ul>
        ///         <li>
        ///             <paramref name="name"/> was empty or only contained whitespace. if no change is wanted, provide the original name
        ///         </li>
        ///         <li><paramref name="hideUntil"/> was more than 90 days in the future</li>
        ///     </ul>
        /// </response>
        /// <response code="403">
        ///     the user is not the creator of the match pool, and lacks the dev permission
        /// </response>
        /// <response code="404">
        ///     no <see cref="MatchPool"/> with <see cref="MatchPool.ID"/> of <paramref name="poolID"/> exists
        /// </response>
        [HttpPost("{poolID}")]
        [Authorize]
        public async Task<ApiResponse<MatchPool>> Update(long poolID,
            [FromQuery] string name,
            [FromQuery] bool unlisted = false,
            [FromQuery] DateTime? hideUntil = null,
            CancellationToken cancel = default
        ) {

            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new Exception($"expected current user to exist here");

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
            if (hideUntil != null && (hideUntil - DateTime.UtcNow) > TimeSpan.FromDays(90)) {
                return ApiBadRequest<MatchPool>($"{nameof(hideUntil)} can only be at most 90 days in the future");
            }

            pool.Name = name;
            pool.Unlisted = unlisted;
            pool.HideUntil = hideUntil;

            _Logger.LogDebug($"updating match pool [poolID={poolID}] [name={pool.Name}] [unlisted={pool.Unlisted}] [hideUntil={pool.HideUntil:u}]");
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

            if (await _CanViewPool(poolID, cancel) == false) {
                return ApiForbidden<List<MatchPoolEntry>>($"no permission to view {nameof(MatchPool)} {poolID} (it is likely hidden)");
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
        /// <response code="200">
        ///     a new <see cref="MatchPoolEntry"/> was successfully created, adding the match to the match pool
        /// </response>
        /// <response code="400">
        ///     a <see cref="MatchPoolEntry"/> with <see cref="MatchPoolEntry.PoolID"/> of <paramref name="poolID"/>
        ///     and <see cref="MatchPoolEntry.MatchID"/> of <paramref name="matchID"/> already exists, meaning
        ///     this would create a duplicate <see cref="MatchPoolEntry"/>
        /// </response>
        /// <response code="403">
        ///     the user making the request is not the creator of the match pool, and lacks the dev permission
        /// </response>
        /// <response code="404">
        ///     one of the following was missing:
        ///     <ul>
        ///         <li>no <see cref="MatchPool"/> with <see cref="MatchPool.ID"/> of <paramref name="poolID"/> exists</li>
        ///         <li>no <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="matchID"/> exists</li> 
        ///     </ul>
        /// </response>
        [HttpPost("{poolID}/{matchID}")]
        [Authorize]
        public async Task<ApiResponse> AddMatchToPool(long poolID, string matchID, CancellationToken cancel = default) {

            if (await _IsPoolCreatorOrDev(poolID, cancel) == false) {
                return ApiForbidden($"user is not the creator of this match pool (or a dev)");
            }

            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new Exception($"expected current user to exist here");

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
        ///     update the <see cref="MatchPoolEntry.Description"/> of a <see cref="MatchPoolEntry"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="MatchPool"/> the entry is in</param>
        /// <param name="matchID">ID of the <see cref="BarMatch"/> the entry is for</param>
        /// <param name="description">new description</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the <see cref="MatchPoolEntry"/> with <see cref="MatchPoolEntry.PoolID"/> of <paramref name="poolID"/>,
        ///     and <see cref="MatchPoolEntry.MatchID"/> of <paramref name="matchID"/> was updated with a new
        ///     <see cref="MatchPoolEntry.Description"/> of <paramref name="description"/>
        /// </response>
        /// <response code="400">
        ///     <paramref name="description"/> was over 100 characters
        /// </response>
        /// <response code="403">
        ///     the user making the request is not the creator of the <see cref="MatchPool"/> with <see cref="MatchPool.ID"/>
        ///     of <paramref name="poolID"/>, and does not have the dev permission
        /// </response>
        /// <response code="404">
        ///     one of the following entries were missing:
        ///     <ul>
        ///         <li>no <see cref="MatchPool"/> with <see cref="MatchPool.ID"/> of <paramref name="poolID"/> exists</li>
        ///         <li>
        ///             no <see cref="MatchPoolEntry"/> with <see cref="MatchPoolEntry.PoolID"/> of <paramref name="poolID"/>
        ///             and <see cref="MatchPoolEntry.MatchID"/> of <paramref name="matchID"/> exists
        ///         </li>
        ///     </ul>
        /// </response>
        [HttpPost("{poolID}/{matchID}/update")]
        [Authorize]
        public async Task<ApiResponse> UpdateEntryDescription(long poolID, string matchID,
            [FromQuery] string description,
            CancellationToken cancel = default
        ) {

            if (description.Length > 100) {
                return ApiBadRequest($"{nameof(description)} cannot be more than 100 characters");
            }

            if (await _IsPoolCreatorOrDev(poolID, cancel) == false) {
                return ApiForbidden($"user is not the creator of the match pool (or a dev)");
            }

            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return ApiNotFound($"{nameof(MatchPool)} {poolID}");
            }

            MatchPoolEntry? entry = await _MatchPoolEntryDb.GetByPoolAndMatchID(poolID, matchID, cancel);
            if (entry == null) {
                return ApiNotFound($"{nameof(MatchPoolEntry)} {poolID} {matchID}");
            }

            entry.Description = description;

            await _MatchPoolEntryDb.UpdateDescription(entry, cancel);
            _Logger.LogInformation($"match pool entry description updated [poolID={poolID}] [matchID={matchID}] [description='{description}']");

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
                ?? throw new Exception($"expected current user to exist here");

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
        private async Task<bool> _IsPoolCreatorOrDev(long poolID, CancellationToken cancel) {
            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new Exception($"expected current user to exist here");

            MatchPool? pool = await _MatchPoolRepository.GetByID(poolID, cancel);
            if (pool == null) {
                return false;
            }

            bool isDev = await _PermissionRepository.HasPermission(currentUser.ID, [AppPermission.GEX_DEV], cancel);

            return isDev == true || pool.CreatedByID == currentUser.ID;
        }

        /// <summary>
        ///     check if a user has permission to view a match pool, which means they have the Gex.Dev permission,
        ///     are the create of the pool, or the HideUntil value has passed
        /// </summary>
        private async Task<bool> _CanViewPool(long poolID, CancellationToken cancel) {
            AppAccount? currentUser = await _CurrentUser.Get(cancel);
            return await _MatchPoolRepository.CanView(poolID, currentUser?.ID, cancel);
        }

    }
}
