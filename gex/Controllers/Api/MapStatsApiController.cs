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
		private readonly MapStatsByFactionDb _FactionStatsDb;

		public MapStatsApiController(ILogger<MapStatsApiController> logger,
			MapStatsDb mapStatsDb, MapStatsStartSpotDb startSpotDb,
			BarMapRepository mapRepository, MapStatsByFactionDb factionStatsDb) {

			_Logger = logger;
			_MapStatsDb = mapStatsDb;
			_StartSpotDb = startSpotDb;
			_MapRepository = mapRepository;
			_FactionStatsDb = factionStatsDb;
		}

		/// <summary>
		///		get the <see cref="MapStatsByGamemode"/> of a map
		/// </summary>
		/// <param name="mapFilename">filename of the map (from <see cref="BarMap.FileName"/></param>
		/// <param name="includeStats">will basic stats be included? defaults to false</param>
		/// <param name="includeStartSpots">will basic stats be included? defaults to false</param>
		/// <param name="includeFactionStats">will basic stats be included? defaults to false</param>
		/// <param name="cancel">cancellation token</param>
		/// <response code="200">
		///		the response will contain a list of <see cref="MapStatsByGamemode"/>s
		///		with <see cref="MapStatsByGamemode.MapFileName"/> of <paramref name="mapFilename"/>
		/// </response>
		[HttpGet("{mapFilename}")]
		public async Task<ApiResponse<MapStats>> GetByMap(string mapFilename,
			[FromQuery] bool includeStats = false,
			[FromQuery] bool includeStartSpots = false,
			[FromQuery] bool includeFactionStats = false,
			CancellationToken cancel = default
		) {

			BarMap? map = await _MapRepository.GetByFileName(mapFilename, cancel);
			if (map == null) {
				return ApiNoContent<MapStats>();
			}

			MapStats stats = new();
			stats.MapFilename = map.FileName;

			if (includeStats == true) {
				stats.Stats = await _MapStatsDb.GetByMap(mapFilename, cancel);
			}

			if (includeStartSpots == true) {
				stats.StartSpots = await _StartSpotDb.GetByMap(mapFilename, cancel);
			}

			if (includeFactionStats == true) {
				stats.FactionStats = await _FactionStatsDb.GetByMap(mapFilename, cancel);
			}

			return ApiOk(stats);
		}

	}
}
