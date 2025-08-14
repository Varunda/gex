using gex.Code.Constants;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Services.BarApi;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private readonly BarSkillLeaderboardRepository _SkillLeaderboardRepository;
        private readonly TeiServerRepository _TeiServerRepository;

        public BarLeaderboardApiController(ILogger<BarLeaderboardApiController> logger,
            BarSkillLeaderboardRepository skillLeaderboardRepository,
            TeiServerRepository teiServerRepository) {

            _Logger = logger;

            _SkillLeaderboardRepository = skillLeaderboardRepository;
            _TeiServerRepository = teiServerRepository;
        }

        /// <summary>
        ///		get the <see cref="BarSkillLeaderboardEntry"/>s
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="BarSkillLeaderboardEntry"/>s
        /// </response>
        [HttpGet("skill")]
        public async Task<ApiResponse<List<BarSkillLeaderboardEntry>>> GetSkillLeaderboard(
            CancellationToken cancel = default
        ) {
            Result<List<BarSeason>, string> seasons = await _TeiServerRepository.GetSeasons(cancel);
            if (seasons.IsOk == false) {
                return ApiInternalError<List<BarSkillLeaderboardEntry>>(seasons.Error);
            }

            int maxSeason = seasons.Value.Select(iter => iter.Season).Max();

            Result<List<BarLeaderboard>, string> leaderboards = await _TeiServerRepository.GetLeaderboard(maxSeason, cancel);
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
                return iter.Take(10);
            }).Aggregate(new List<BarSkillLeaderboardEntry>(), (acc, iter) => {
                acc.AddRange(iter);
                return acc;
            }).ToList();

            return ApiOk(f);
        }

    }
}
