using gex.Models;
using gex.Models.Bar;
using gex.Services.Db.Bar;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        ///     get an aggregated list of all tweaks used, where <see cref="BarTweakEntry.Count"/> spans the entire
        ///     lifetime of Gex, rather than a single day. this means each <see cref="BarTweakEntry.Hash"/> will only
        ///     be included once
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarTweakEntry"/>s
        /// </response>
        [HttpGet("aggregate")]
        public async Task<ApiResponse<List<BarTweakEntry>>> GetAggregate(CancellationToken cancel = default) {
            List<BarTweakEntry> tweaks = await _TweakEntryRepository.GetAll(cancel);

            Dictionary<string, BarTweakEntry> dict = [];

            foreach (BarTweakEntry entry in tweaks) {
                if (dict.ContainsKey(entry.Hash) == false) {
                    BarTweakEntry copy = new();
                    copy.Hash = entry.Hash;
                    copy.Day = entry.Day;
                    copy.Type = entry.Type;
                    copy.Name = entry.Name;
                    copy.Base64 = entry.Base64;
                    copy.Count = 0;

                    dict[entry.Hash] = copy;
                }

                dict[entry.Hash].Count += entry.Count;
            }

            return ApiOk(dict.Values.ToList());
        }

    }
}
