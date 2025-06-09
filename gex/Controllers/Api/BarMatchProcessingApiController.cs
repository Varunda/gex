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
    [Route("/api/match-processing")]
    public class BarMatchProcessingApiController : ApiControllerBase {

        private readonly ILogger<BarMatchProcessingApiController> _Logger;
        private readonly BarMatchProcessingRepository _ProcessingRepository;

        public BarMatchProcessingApiController(ILogger<BarMatchProcessingApiController> logger,
            BarMatchProcessingRepository processingRepository) {

            _Logger = logger;
            _ProcessingRepository = processingRepository;
        }

        /// <summary>
        ///		get the <see cref="BarMatchProcessing"/> of a match
        /// </summary>
        /// <param name="gameID">ID of the <see cref="BarMatchProcessing"/> to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain the <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/>
        ///		of <paramref name="gameID"/>
        /// </response>
        /// <response code="204">
        ///		no <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/> of <paramref name="gameID"/> exists
        /// </response>
        [HttpGet("{gameID}")]
        public async Task<ApiResponse<BarMatchProcessing>> GetByGameID(string gameID, CancellationToken cancel) {
            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                return ApiNoContent<BarMatchProcessing>();
            }

            return ApiOk(proc);
        }

        /// <summary>
        ///		get a list of the top 100 games to be processed via the priority system
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		a list of <see cref="BarMatchProcessing"/> that represents an ordered list of games
        ///		as they will be prioritized. note that this list can change, and is not set in stone
        /// </response>
        [HttpGet("priority")]
        public async Task<ApiResponse<List<BarMatchProcessing>>> GetPriorityList(CancellationToken cancel) {
            List<BarMatchProcessing> list = await _ProcessingRepository.GetPriorityList(cancel);
            return ApiOk(list);
        }

    }
}
