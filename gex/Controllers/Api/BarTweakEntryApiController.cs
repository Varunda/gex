using gex.Models;
using gex.Models.Bar;
using gex.Services.Db.Bar;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/tweak-entry")]
    public class BarTweakEntryApiController : ApiControllerBase {

        private readonly ILogger<BarTweakEntryApiController> _Logger;
        private readonly BarTweakEntryRepository _TweakEntryRepository;

        public BarTweakEntryApiController(ILogger<BarTweakEntryApiController> logger,
            BarTweakEntryRepository tweakEntryRepository) {

            _Logger = logger;
            _TweakEntryRepository = tweakEntryRepository;
        }

        /// <summary>
        ///     get a list of all <see cref="BarTweakEntry"/>s stored in the repository
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarTweakEntry"/>s
        /// </response>
        [HttpGet("all")]
        public async Task<ApiResponse<List<BarTweakEntry>>> GetAll(CancellationToken cancel = default) {
            List<BarTweakEntry> tweaks = await _TweakEntryRepository.GetAll(cancel);

            return ApiOk(tweaks);
        }

    }
}
