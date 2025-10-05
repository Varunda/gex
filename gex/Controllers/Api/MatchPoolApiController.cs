using gex.Code;
using gex.Models;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Services;
using gex.Services.Db;
using gex.Services.Repositories;
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

        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;
        private readonly BarMatchRepository _MatchRepository;

        public MatchPoolApiController(ILogger<MatchPoolApiController> logger,
            MatchPoolRepository matchPoolRepository, MatchPoolEntryDb matchPoolEntryDb,
            ICurrentAccount currentUser, BarMatchRepository matchRepository) {

            _Logger = logger;
            _MatchPoolRepository = matchPoolRepository;
            _MatchPoolEntryDb = matchPoolEntryDb;
            _CurrentUser = currentUser;
            _MatchRepository = matchRepository;
        }

        /// <summary>
        ///     get all <see cref="MatchPool"/>s
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of all <see cref="MatchPool"/>s
        /// </response>
        [HttpGet]
        public async Task<ApiResponse<List<MatchPool>>> GetPools(CancellationToken cancel = default) {
            List<MatchPool> pools = await _MatchPoolRepository.GetAll(cancel);
            return ApiOk(pools);
        }

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
        [PermissionNeeded(AppPermission.GEX_MATCH_POOL_ENTRY_ADDREMOVE)]
        public async Task<ApiResponse> AddMatchToPool(long poolID, string matchID, CancellationToken cancel = default) {
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
        [PermissionNeeded(AppPermission.GEX_MATCH_POOL_ENTRY_ADDREMOVE)]
        public async Task<ApiResponse> RemoveMatchFromPool(long poolID, string matchID, CancellationToken cancel = default) {
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

    }
}
