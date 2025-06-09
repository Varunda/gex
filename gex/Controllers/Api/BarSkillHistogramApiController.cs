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
    [Route("api/skill-histogram")]
    public class BarSkillHistogramApiController : ApiControllerBase {

        private readonly ILogger<BarSkillHistogramApiController> _Logger;
        private readonly SkillHistogramRepository _Repository;

        public BarSkillHistogramApiController(ILogger<BarSkillHistogramApiController> logger,
            SkillHistogramRepository repository) {

            _Logger = logger;
            _Repository = repository;
        }

        /// <summary>
        ///		get the skill histogram. minimum of 25 games played to be included.
        ///		only duels, small team and large team are included
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="SkillHistogramEntry"/>s
        ///		that represent the historgram of player skill
        /// </response>
        [HttpGet]
        public async Task<ApiResponse<List<SkillHistogramEntry>>> Get(
            CancellationToken cancel = default
        ) {
            return ApiOk(await _Repository.Get(cancel));
        }

    }
}
