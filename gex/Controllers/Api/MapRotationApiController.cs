using gex.Common.Models;
using gex.Models;
using gex.Models.Bar;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/map-rotation")]
    public class MapRotationApiController : ApiControllerBase {

        private readonly ILogger<MapRotationApiController> _Logger;
        private readonly BarMapRotationRepository _MapRotationRepository;

        public MapRotationApiController(ILogger<MapRotationApiController> logger,
            BarMapRotationRepository mapRotationRepository) {

            _Logger = logger;
            _MapRotationRepository = mapRotationRepository;
        }

        [HttpGet]
        public async Task<ApiResponse<List<BarMapRotation>>> Get(CancellationToken cancel = default) {
            Result<List<BarMapRotation>, string> rotations = await _MapRotationRepository.GetAll(cancel);

            if (rotations.IsOk == false) {
                return ApiInternalError<List<BarMapRotation>>($"error loading map rotations: {rotations.Error}]");
            }

            return ApiOk(rotations.Value);
        }

    }
}
