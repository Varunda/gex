using gex.Models;
using gex.Models.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace gex.Controllers.Api.Account {

    [ApiController]
    [Route("/api/permission")]
    public class AppPermissionApiController : ApiControllerBase {

        private readonly ILogger<AppPermissionApiController> _Logger;

        public AppPermissionApiController(ILogger<AppPermissionApiController> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     Get all permissions available to users
        /// </summary>
        /// <response code="200">
        ///     A list of <see cref="AppPermission"/>s
        /// </response>
        [HttpGet]
        public ApiResponse<List<AppPermission>> GetAll() {
            return ApiOk(AppPermission.All);
        }

    }
}
