using gex.Models;
using gex.Models.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

	[ApiController]
	[Route("/api/leaderboard")]
	public class BarLeaderboardApiController : ApiControllerBase {

		private readonly ILogger<BarLeaderboardApiController> _Logger;
		private readonly BarSkillLeaderboardRepository _SkillLeaderboardRepository;

		public BarLeaderboardApiController(ILogger<BarLeaderboardApiController> logger,
			BarSkillLeaderboardRepository skillLeaderboardRepository) {

			_Logger = logger;

			_SkillLeaderboardRepository = skillLeaderboardRepository;
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

			return ApiOk(await _SkillLeaderboardRepository.Get(cancel));
		}

	}
}
