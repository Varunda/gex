using gex.Models;
using gex.Models.Api;
using gex.Models.UserStats;
using gex.Services.Db.UserStats;
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

        public UserApiController(ILogger<UserApiController> logger,
            BarUserDb userDb, BarUserMapStatsDb mapStatsDb,
            BarUserFactionStatsDb factionStatsDb, BarUserSkillDb skillDb) {

            _Logger = logger;
            _UserDb = userDb;
            _MapStatsDb = mapStatsDb;
            _FactionStatsDb = factionStatsDb;
            _SkillDb = skillDb;
        }

        /// <summary>
        ///		get user info. if <see cref="ApiBarUser.Skill"/>, <see cref="ApiBarUser.MapStats"/> or <see cref="ApiBarUser.FactionStats"/>
        ///		is wanted, make sure to set the corresponding parameter to <c>true</c>
        /// </summary>
        /// <param name="userID">ID of the user to get</param>
        /// <param name="includeSkill">if <see cref="ApiBarUser.Skill"/> is populated or not. defaults to false</param>
        /// <param name="includeMapStats">if <see cref="ApiBarUser.MapStats"/> is populated or not. defaults to false</param>
        /// <param name="includeFactionStats">if <see cref="ApiBarUser.FactionStats"/> is populated or not. defauls to false</param>
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

            return ApiOk(response);
        }

        /// <summary>
        ///		search users based on current username (case-insensitive).
        ///		<see cref="ApiBarUser.FactionStats"/> and <see cref="ApiBarUser.MapStats"/> is never populated
        /// </summary>
        /// <param name="search">text to search for. must be at least 3 characters long</param>
        /// <param name="includeSkill">if <see cref="ApiBarUser.Skill"/> will be populated. defaults to false</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="ApiBarUser"/>s that match <paramref name="search"/>,
        ///		and will include <see cref="ApiBarUser.Skill"/> if <paramref name="includeSkill"/> is true
        /// </response>
        /// <response code="400">
        ///		<paramref name="search"/> is not at least 3 characters long
        /// </response>
        [HttpGet("search/{search}")]
        public async Task<ApiResponse<List<ApiBarUser>>> Search(string search,
            [FromQuery] bool includeSkill = false,
            CancellationToken cancel = default
        ) {

            if (search.Length < 3) {
                return ApiBadRequest<List<ApiBarUser>>($"search must be at least 3 characters");
            }

            List<BarUser> users = await _UserDb.SearchByName(search, cancel);

            List<ApiBarUser> ret = [];
            foreach (BarUser user in users) {
                ApiBarUser response = new();
                response.UserID = user.UserID;
                response.Username = user.Username;
                response.LastUpdated = user.LastUpdated;

                if (includeSkill == true) {
                    response.Skill = await _SkillDb.GetByUserID(user.UserID, cancel);
                }
                ret.Add(response);
            }

            return ApiOk(ret);
        }

    }
}
