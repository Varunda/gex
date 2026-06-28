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

        /// <summary>
        ///     get 30 days of <see cref="BarMapPlayCountEntry"/> data, where each map is aggregated
        ///     across each day and each gamemode. use <see cref="GetAllTime(CancellationToken)"/>
        ///     if all time data is wanted
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the reponse will contain a list of <see cref="BarMapPlayCountEntry"/>s that represents the 
        ///     daily plays of each map across each gamemode
        /// </response>
        [HttpGet("recent")]
        public async Task<ApiResponse<List<BarMapPlayCountEntry>>> GetWithDate(CancellationToken cancel = default) {
            return ApiOk(await _Repository.GetWithDate(cancel));
        }

        /// <summary>
        ///     get the all time map counts, which is aggregated across each gamemode, and cannot
        ///     be broken into per day data
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will cont a list of <see cref="BarMapPlayCountEntry"/>s that represent
        ///     the total play count for each map across each gamemode
        /// </response>
        [HttpGet("recent/alltime")]
        public async Task<ApiResponse<List<BarMapPlayCountEntry>>> GetAllTime(CancellationToken cancel = default) {
            return ApiOk(await _Repository.GetAllTime(cancel));
        }

    }
}
