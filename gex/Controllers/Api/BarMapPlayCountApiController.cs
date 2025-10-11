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
    [Route("/api/map-play-count")]
    public class BarMapPlayCountApiController : ApiControllerBase {

        private readonly ILogger<BarMapPlayCountApiController> _Logger;
        private readonly BarMapPlayCountRepository _Repository;

        public BarMapPlayCountApiController(ILogger<BarMapPlayCountApiController> logger,
            BarMapPlayCountRepository repository) {

            _Logger = logger;
            _Repository = repository;
        }

        /// <summary>
        ///		get maps plays for each gamemode within 24 hours
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="BarMapPlayCountEntry"/>s,
        ///		each one representing a top map played within a gamemode
        /// </response>
        [HttpGet("recent/daily")]
        public async Task<ApiResponse<List<BarMapPlayCountEntry>>> GetDaily(CancellationToken cancel = default) {
            return ApiOk(await _Repository.GetDaily(cancel));

        }

        /// <summary>
        ///		get maps plays for each gamemode within 24 hours
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="BarMapPlayCountEntry"/>s,
        ///		each one representing a top map played within a gamemode
        /// </response>
        [HttpGet("recent/7day")]
        public async Task<ApiResponse<List<BarMapPlayCountEntry>>> Get7Days(CancellationToken cancel = default) {
            return ApiOk(await _Repository.Get7Day(cancel));
        }

        /// <summary>
        ///		get maps plays for each gamemode within 24 hours
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="BarMapPlayCountEntry"/>s,
        ///		each one representing a top map played within a gamemode
        /// </response>
        [HttpGet("recent/30day")]
        public async Task<ApiResponse<List<BarMapPlayCountEntry>>> Get30Days(CancellationToken cancel = default) {
            return ApiOk(await _Repository.Get30Day(cancel));
        }

    }
}
