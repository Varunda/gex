using gex.Models;
using gex.Models.MapStats;
using gex.Services.Db.MapStats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

	[ApiController]
	[Route("/api/map-stats")]
	public class MapStatsApiController : ApiControllerBase {

		private readonly ILogger<MapStatsApiController> _Logger;
		private readonly MapStatsDb _MapStatsDb;

		public MapStatsApiController(ILogger<MapStatsApiController> logger,
			MapStatsDb mapStatsDb) {

			_Logger = logger;
			_MapStatsDb = mapStatsDb;
		}

		[HttpGet("{mapFilename}")]
		public async Task<ApiResponse<List<MapStatsByGamemode>>> GetByMap(string mapFilename,
			CancellationToken cancel) {

			List<MapStatsByGamemode> stats = await _MapStatsDb.GetByMap(mapFilename, cancel);

			return ApiOk(stats);
		}


	}
}
