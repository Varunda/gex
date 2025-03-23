using gex.Models;
using gex.Models.Api;
using gex.Models.UserStats;
using gex.Services.Db.UserStats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

    }
}
