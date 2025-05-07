using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using gex.Models;
using gex.Services;
using gex.Code;
using gex.Models.Internal;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/debug")]
    [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
    public class DebugApiController : ApiControllerBase {

        private readonly ILogger<DebugApiController> _Logger;
        private readonly HttpUtilService _HttpUtil;
        private readonly IHttpContextAccessor _HttpContext;

        public DebugApiController(ILogger<DebugApiController> logger,
            HttpUtilService httpUti, IHttpContextAccessor httpContext) {

            _Logger = logger;
            _HttpUtil = httpUti;
            _HttpContext = httpContext;
        }

        /// <summary>
        ///     debug method to get the IP of the request
        /// </summary>
        /// <response code="200">
        ///     a string representing the IP of the client making the request, or "missing?" if for some reason no IP could be found
        /// </response>
        [HttpGet("remote-ip")]
        public ApiResponse<string> GetRequestIp() {
            string? ip = _HttpUtil.GetHttpRemoteIp(_HttpContext.HttpContext);
            return ApiOk(ip ?? "missing?");
        }

    }
}
