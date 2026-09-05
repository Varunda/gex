using gex.Code;
using gex.Common.Services.Db;
using gex.Models;
using gex.Models.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/test-error")]
    [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
    public class TestErrorApiController : ApiControllerBase {

        private readonly ILogger<TestErrorApiController> _Logger;
        private readonly IDbHelper _DbHelper;

        public TestErrorApiController(ILogger<TestErrorApiController> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     throw an exception due to a DB timeout
        /// </summary>
        /// <response code="500">
        ///     always
        /// </response>
        [HttpGet("db-timeout")]
        public async Task<ApiResponse<int>> DbTimeout() {
            using DbConnection conn = _DbHelper.Connection();
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                SELECT pg_sleep(10);
            ");

            cmd.CommandTimeout = 1;

            await cmd.ExecuteNonQueryAsync();

            return ApiOk(0);
        }

    }
}
