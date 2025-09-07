using gex.Code;
using gex.Models;
using gex.Models.Internal;
using gex.Services.Repositories.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api.Account {

    [Route("/api/group")]
    [ApiController]
    public class AppGroupApiController : ApiControllerBase {

        private readonly ILogger<AppGroupApiController> _Logger;
        private readonly AppGroupRepository _AppGroupRepository;

        public AppGroupApiController(ILogger<AppGroupApiController> logger,
            AppGroupRepository appGroupRepository) {

            _Logger = logger;
            _AppGroupRepository = appGroupRepository;
        }

        /// <summary>
        ///		get all <see cref="AppGroup"/>s within the app
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ApiResponse<List<AppGroup>>> GetAll(CancellationToken cancel) {
            List<AppGroup> groups = await _AppGroupRepository.GetAll(cancel);

            return ApiOk(groups);
        }

        /// <summary>
        ///		create a new <see cref="AppGroup"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="hex"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> Create([FromQuery] string name, [FromQuery] string hex,
            CancellationToken cancel) {

            AppGroup group = new();
            group.Name = name;

            _Logger.LogInformation($"creating new {nameof(AppGroup)} [name={name}]");
            group.ID = await _AppGroupRepository.Insert(group, cancel);

            return ApiOk();
        }

    }
}
