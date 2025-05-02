using gex.Models;
using gex.Models.Api;
using gex.Models.Bar;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

	[Route("/api/match-search")]
	[ApiController]
	public class MatchSearchApiController : ApiControllerBase {

		private readonly ILogger<MatchSearchApiController> _Logger;
		private readonly BarMatchRepository _MatchRepository;
		private readonly BarMapRepository _MapRepository;

		public MatchSearchApiController(ILogger<MatchSearchApiController> logger,
			BarMatchRepository matchRepository, BarMapRepository mapRepository) {

			_Logger = logger;
			_MatchRepository = matchRepository;
			_MapRepository = mapRepository;
		}

		/// <summary>
		///		get a list of unique engines for all matches stored in the DB
		/// </summary>
		/// <param name="cancel">cancellation token</param>
		/// <response code="200">
		///		the response will contain a list of <see cref="SearchResult"/>s that
		///		contain the list of unique engines used across all games in the DB
		/// </response>
		[HttpGet("engines")]
		public async Task<ApiResponse<List<SearchResult>>> GetUniqueEngines(CancellationToken cancel) {
			List<string> unq = await _MatchRepository.GetUniqueEngines(cancel);
			List<SearchResult> results = unq.Select(iter => new SearchResult() { Value = iter }).ToList();
			return ApiOk(results);
		}

		[HttpGet("game-versions")]
		public async Task<ApiResponse<List<SearchResult>>> GetUniqueGameVersions(CancellationToken cancel) {
			List<string> unq = await _MatchRepository.GetUniqueGameVersions(cancel);
			List<SearchResult> results = unq.Select(iter => new SearchResult() { Value = iter }).ToList();
			return ApiOk(results);
		}

		[HttpGet("maps")]
		public async Task<ApiResponse<List<SearchResult>>> GetUniqueMaps(CancellationToken cancel) {
			List<BarMap> unq = await _MapRepository.GetAll(cancel);
			List<SearchResult> results = unq.Select(iter => new SearchResult() { Value = iter.Name }).ToList();
			return ApiOk(results);
		}

	}
}
