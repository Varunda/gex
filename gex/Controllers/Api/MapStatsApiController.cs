using gex.Models;
using gex.Models.Bar;
using gex.Models.MapStats;
using gex.Services.Db.MapStats;
using gex.Services.Repositories;
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
		private readonly BarMapRepository _MapRepository;
		private readonly MapStatsDb _MapStatsDb;
		private readonly MapStatsStartSpotDb _StartSpotDb;

		public MapStatsApiController(ILogger<MapStatsApiController> logger,
			MapStatsDb mapStatsDb, MapStatsStartSpotDb startSpotDb,
			BarMapRepository mapRepository) {

			_Logger = logger;
			_MapStatsDb = mapStatsDb;
			_StartSpotDb = startSpotDb;
			_MapRepository = mapRepository;
		}

		/// <summary>
		///		get the <see cref="MapStatsByGamemode"/> of a map
		/// </summary>
		/// <param name="mapFilename">filename of the map (from <see cref="BarMap.FileName"/></param>
		/// <param name="cancel">cancellation token</param>
		/// <response code="200">
		///		the response will contain a list of <see cref="MapStatsByGamemode"/>s
		///		with <see cref="MapStatsByGamemode.MapFileName"/> of <paramref name="mapFilename"/>
		/// </response>
		[HttpGet("{mapFilename}")]
		public async Task<ApiResponse<MapStats>> GetByMap(string mapFilename,
			CancellationToken cancel) {

			BarMap? map = await _MapRepository.GetByFileName(mapFilename, cancel);
			if (map == null) {
				return ApiNoContent<MapStats>();
			}

			MapStats stats = new();
			stats.Stats = await _MapStatsDb.GetByMap(mapFilename, cancel);
			stats.StartSpots = await _StartSpotDb.Get(mapFilename, cancel);

			return ApiOk(stats);
		}

	}
}
