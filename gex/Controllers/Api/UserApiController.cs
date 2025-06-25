using gex.Models;
using gex.Models.Api;
using gex.Models.Bar;
using gex.Models.MapStats;
using gex.Models.UserStats;
using gex.Services.Db;
using gex.Services.Db.UserStats;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [Route("/api/user")]
    [ApiController]
    public class UserApiController : ApiControllerBase {

        private readonly ILogger<UserApiController> _Logger;
        private readonly BarUserDb _UserDb;
        private readonly BarUserSkillDb _SkillDb;
        private readonly BarUserMapStatsDb _MapStatsDb;
        private readonly BarUserFactionStatsDb _FactionStatsDb;
        private readonly BarMapDb _MapDb;
        private readonly MapStatsStartSpotRepository _StartSpotRepository;

        public UserApiController(ILogger<UserApiController> logger,
            BarUserDb userDb, BarUserMapStatsDb mapStatsDb,
            BarUserFactionStatsDb factionStatsDb, BarUserSkillDb skillDb,
            BarMapDb mapDb, MapStatsStartSpotRepository startSpotRepository) {

            _Logger = logger;
            _UserDb = userDb;
            _MapStatsDb = mapStatsDb;
            _FactionStatsDb = factionStatsDb;
            _SkillDb = skillDb;
            _MapDb = mapDb;
            _StartSpotRepository = startSpotRepository;
        }

        /// <summary>
        ///		get user info. if <see cref="ApiBarUser.Skill"/>, <see cref="ApiBarUser.MapStats"/> or <see cref="ApiBarUser.FactionStats"/>
        ///		is wanted, make sure to set the corresponding parameter to <c>true</c>
        /// </summary>
        /// <param name="userID">ID of the user to get</param>
        /// <param name="includeSkill">if <see cref="ApiBarUser.Skill"/> is populated or not. defaults to false</param>
        /// <param name="includeMapStats">if <see cref="ApiBarUser.MapStats"/> is populated or not. defaults to false</param>
        /// <param name="includeFactionStats">if <see cref="ApiBarUser.FactionStats"/> is populated or not. defaults to false</param>
        /// <param name="includePreviousNames">if <see cref="ApiBarUser.PreviousNames"/> will be populated or not. defaults to false</param>
        /// <param name="cancel"></param>
        /// <response code="200">
        ///		the response will contain the <see cref="ApiBarUser"/> with <see cref="ApiBarUser.UserID"/> of <paramref name="userID"/>
        /// </response>
        /// <response code="204">
        ///		no <see cref="ApiBarUser"/> with <see cref="ApiBarUser.UserID"/> of <paramref name="userID"/> exists
        /// </response>
        [HttpGet("{userID}")]
        public async Task<ApiResponse<ApiBarUser>> GetByUserID(long userID,
            [FromQuery] bool includeSkill = false,
            [FromQuery] bool includeMapStats = false,
            [FromQuery] bool includeFactionStats = false,
            [FromQuery] bool includePreviousNames = false,
            CancellationToken cancel = default
        ) {

            BarUser? user = await _UserDb.GetByID(userID, cancel);
            if (user == null) {
                return ApiNoContent<ApiBarUser>();
            }

            ApiBarUser response = new();
            response.UserID = user.UserID;
            response.Username = user.Username;

            if (includeSkill == true) {
                response.Skill = await _SkillDb.GetByUserID(userID, cancel);
            }

            if (includeMapStats == true) {
                response.MapStats = await _MapStatsDb.GetByUserID(userID, cancel);
            }

            if (includeFactionStats == true) {
                response.FactionStats = await _FactionStatsDb.GetByUserID(userID, cancel);
            }

            if (includePreviousNames == true) {
                response.PreviousNames = await _UserDb.GetUserNames(userID, cancel);
            }

            return ApiOk(response);
        }

        /// <summary>
        ///		search users based on current username (case-insensitive).
        ///		<see cref="ApiBarUser.FactionStats"/> and <see cref="ApiBarUser.MapStats"/> is never populated
        /// </summary>
        /// <param name="search">text to search for. must be at least 3 characters long</param>
        /// <param name="searchPreviousNames">will previous names be searched against as well? defaults to false</param>
        /// <param name="includeSkill">if <see cref="ApiBarUser.Skill"/> will be populated. defaults to false</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="UserSearchResult"/>s that match <paramref name="search"/>,
        ///		and will include <see cref="UserSearchResult.Skill"/> if <paramref name="includeSkill"/> is true,
        ///		and if <paramref name="searchPreviousNames"/> is true, the <see cref="UserSearchResult.Username"/>
        ///		might not match <paramref name="search"/>, but <see cref="UserSearchResult.PreviousName"/> will
        /// </response>
        /// <response code="400">
        ///		<paramref name="search"/> is not at least 3 characters long
        /// </response>
        [HttpGet("search/{search}")]
        public async Task<ApiResponse<List<UserSearchResult>>> Search(string search,
            [FromQuery] bool includeSkill = false,
            [FromQuery] bool searchPreviousNames = false,
            CancellationToken cancel = default
        ) {

            if (search.Length < 3) {
                return ApiBadRequest<List<UserSearchResult>>($"search must be at least 3 characters");
            }

            List<UserSearchResult> users = await _UserDb.SearchByName(search, searchPreviousNames, cancel);

            foreach (UserSearchResult user in users) {
                if (includeSkill == true) {
                    user.Skill = await _SkillDb.GetByUserID(user.UserID, cancel);
                }
            }

            return ApiOk(users);
        }

        /// <summary>
        ///     get the start spots of a user on a specific map, either by map name or map filename
        /// </summary>
        /// <param name="userID">ID of the user</param>
        /// <param name="mapFilename">filename of the map. either this one or <paramref name="mapName"/> must be given</param>
        /// <param name="mapName">name of the map. either this or <paramref name="mapFilename"/> must be given</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="MapStatsStartSpot"/>s
        ///     for the user on the map
        /// </response>
        /// <response code="400">
        ///     neither <paramref name="mapFilename"/> or <paramref name="mapName"/> was given
        /// </response>
        /// <response code="404">
        ///     one of the following objects could not be found:
        ///     <ul>
        ///         <li>no <see cref="BarUser"/> with <see cref="BarUser.UserID"/> of <paramref name="userID"/> exists</li>
        ///         <li>no <see cref="BarMap"/> with <see cref="BarMap.FileName"/> of <paramref name="mapFilename"/> exists</li>
        ///     </ul>
        /// </response>
        [HttpGet("start-spots/{userID}")]
        public async Task<ApiResponse<List<MapStatsStartSpot>>> GetStartSpotsByName(
            long userID,
            [FromQuery] string? mapFilename = null,
            [FromQuery] string? mapName = null,
            CancellationToken cancel = default
        ) {

            if (mapFilename == null && mapName == null) {
                return ApiBadRequest<List<MapStatsStartSpot>>($"either {nameof(mapFilename)} or {nameof(mapName)} must be given");
            }
            if (mapFilename != null && mapName != null) {
                return ApiBadRequest<List<MapStatsStartSpot>>($"cannot give both {nameof(mapFilename)} and {nameof(mapName)}");
            }

            BarMap? map = null;

            if (mapFilename != null) {
                map = await _MapDb.GetByFileName(mapFilename, cancel);
                if (map == null) {
                    return ApiNotFound<List<MapStatsStartSpot>>($"{nameof(BarMap)} {mapFilename}");
                }
            }

            if (mapName != null) {
                map = await _MapDb.GetByName(mapName, cancel);
                if (map == null) {
                    return ApiNotFound<List<MapStatsStartSpot>>($"{nameof(BarMap)} {mapName}");
                }
            }

            if (map == null) {
                throw new System.Exception($"logic error: why is map null here, 404 was expected");
            }

            BarUser? user = await _UserDb.GetByID(userID, cancel);
            if (user == null) {
                return ApiNotFound<List<MapStatsStartSpot>>($"{nameof(BarUser)} {userID}");
            }

            List<MapStatsStartSpot> spots = await _StartSpotRepository.GetByMapAndUser(map.FileName, userID, cancel);
            return ApiOk(spots);
        }

    }
}
