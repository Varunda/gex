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

		/// <summary>
		///		get a <see cref="BarMap"/> by its <see cref="BarMap.FileName"/>
		/// </summary>
		/// <param name="filename">filename of the map to get</param>
		/// <param name="cancel">cancellation token</param>
		/// <response code="200">
		///		the response will contain the <see cref="BarMap"/> with the <see cref="BarMap.FileName"/>
		///		of <paramref name="filename"/>
		/// </response>
		/// <response code="204">
		///		no <see cref="BarMap"/> with <see cref="BarMap.FileName"/> of <paramref name="filename"/> exists
		/// </response>
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
