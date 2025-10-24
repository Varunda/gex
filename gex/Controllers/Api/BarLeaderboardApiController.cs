using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/leaderboard")]
    public class BarLeaderboardApiController : ApiControllerBase {

        private readonly ILogger<BarLeaderboardApiController> _Logger;
        private readonly TeiServerRepository _TeiServerRepository;

        public BarLeaderboardApiController(ILogger<BarLeaderboardApiController> logger,
            TeiServerRepository teiServerRepository) {

            _Logger = logger;

            _TeiServerRepository = teiServerRepository;
        }

        /// <summary>
        ///		get the <see cref="BarSkillLeaderboardEntry"/>s
        /// </summary>
        /// <param name="count">how many in the leaderboard to return. valid range: 1-100 (inclusive)</param>
        /// <param name="season">what season to view the leaderboards of. pass null or a negative value to use the current season</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="BarSkillLeaderboardEntry"/>s
        /// </response>
        /// <response code="400">
        ///     <paramref name="count"/> was not between 1-100 (inclusive)
        /// </response>
        [HttpGet("skill")]
        public async Task<ApiResponse<List<BarSkillLeaderboardEntry>>> GetSkillLeaderboard(
            [FromQuery] int count = 10,
            [FromQuery] int? season = null,
            CancellationToken cancel = default
        ) {
            if (count <= 0 || count > 100) {
                return ApiBadRequest<List<BarSkillLeaderboardEntry>>($"{nameof(count)} must be between 1 and 100 (inclusive)");
            }

            if (season == null || season < 0) {
                Result<List<BarSeason>, string> seasons = await _TeiServerRepository.GetSeasons(cancel);
                if (seasons.IsOk == false) {
                    return ApiInternalError<List<BarSkillLeaderboardEntry>>(seasons.Error);
                }

                season = seasons.Value.Select(iter => iter.Season).Max();
            }

            Result<List<BarLeaderboard>, string> leaderboards = await _TeiServerRepository.GetLeaderboard(season.Value, cancel);
            if (leaderboards.IsOk == false) {
                return ApiInternalError<List<BarSkillLeaderboardEntry>>(leaderboards.Error);
            }

            // only want the top 10 per gamemode
            Dictionary<byte, List<BarSkillLeaderboardEntry>> entries = [];

            foreach (BarLeaderboard lb in leaderboards.Value) {
                byte gamemode = BarGamemode.GetID(lb.Gamemode);
                if (entries.ContainsKey(gamemode) == false) {
                    entries.Add(gamemode, []);
                }

                foreach (BarLeaderboardPlayer p in lb.Players) {
                    BarSkillLeaderboardEntry e = new();
                    e.Skill = p.Rating;
                    e.Username = p.Username;
                    e.UserID = p.UserID;
                    e.Gamemode = BarGamemode.GetID(lb.Gamemode);

                    entries[gamemode].Add(e);
                }
            }

            List<BarSkillLeaderboardEntry> f = new List<List<BarSkillLeaderboardEntry>>(entries.Values).Select(iter => {
                return iter.Take(count);
            }).Aggregate(new List<BarSkillLeaderboardEntry>(), (acc, iter) => {
                acc.AddRange(iter);
                return acc;
            }).ToList();

            return ApiOk(f);
        }

        /// <summary>
        ///     get the <see cref="BarSeason"/>s available
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        [HttpGet("seasons")]
        public async Task<ApiResponse<List<BarSeason>>> GetSeasons(CancellationToken cancel) {
            Result<List<BarSeason>, string> seasons = await _TeiServerRepository.GetSeasons(cancel);
            if (seasons.IsOk == false) {
                return ApiInternalError<List<BarSeason>>(seasons.Error);
            }

            return ApiOk(seasons.Value);
        }

    }
}
