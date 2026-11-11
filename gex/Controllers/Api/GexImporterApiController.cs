using gex.Code;
using gex.Models;
using gex.Models.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/gex-import")]
    public class GexImporterApiController : ApiControllerBase {

        private readonly ILogger<GexImporterApiController> _Logger;

        private static HttpClient _Http = new();

        static GexImporterApiController() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex-importer/0.1");
        }

        public GexImporterApiController(ILogger<GexImporterApiController> logger) {
            _Logger = logger;
        }

        [HttpPost("match/{gameID}")]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        [Authorize]
        public async Task<ApiResponse> ImportMatch(string gexUrl, string gameID, CancellationToken cancel) {
            using HttpResponseMessage apiRes = await _Http.GetAsync($"{gexUrl}/api/match/{gameID}", cancel);

            using MemoryStream ms = new();
            await apiRes.Content.CopyToAsync(ms, cancel);

            string body = Encoding.UTF8.GetString(ms.ToArray());

            if (apiRes.StatusCode != System.Net.HttpStatusCode.OK) {
                return ApiBadRequest(body);
            }

            throw new NotImplementedException();
        }

    }
}
