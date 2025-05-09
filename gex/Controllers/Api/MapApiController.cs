using gex.Models;
using gex.Models.Bar;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

	[ApiController]
	[Route("/api/map")]
	public class MapApiController : ApiControllerBase {

		private readonly ILogger<MapApiController> _Logger;
		private readonly BarMapRepository _MapRepository;

		public MapApiController(ILogger<MapApiController> logger,
			BarMapRepository mapRepository) {

			_Logger = logger;
			_MapRepository = mapRepository;
		}

		[HttpGet("{filename}")]
		public async Task<ApiResponse<BarMap>> Get(string filename,
			CancellationToken cancel) {

			BarMap? map = await _MapRepository.GetByFileName(filename, cancel);
			if (map == null) {
				return ApiNoContent<BarMap>();
			}

			return ApiOk(map);
		}

	}
}
