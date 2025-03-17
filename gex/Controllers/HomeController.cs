using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using gex.Code;
using gex.Services;
using Microsoft.Extensions.Options;
using gex.Models.Options;
using System.IO;
using gex.Services.Repositories;
using gex.Models.Db;
using System.Threading.Tasks;
using System.Net.Mime;

namespace gex.Controllers {

    public class HomeController : Controller {

        private readonly ILogger<HomeController> _Logger;

        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly HttpUtilService _HttpUtil;
        private readonly IOptions<FileStorageOptions> _Options;

        private readonly BarMatchRepository _MatchRepository;

        public HomeController(ILogger<HomeController> logger,
            IHttpContextAccessor httpContextAccessor, HttpUtilService httpUtil,
            IOptions<FileStorageOptions> options, BarMatchRepository matchRepository) {

            _HttpContextAccessor = httpContextAccessor;
            _HttpUtil = httpUtil;
            _Logger = logger;
            _Options = options;
            _MatchRepository = matchRepository;
        }

        public IActionResult Index() {
            return View();
        }

        [Authorize]
        [AccountRequired]
        public IActionResult AccountManagement() {
            return View();
        }

        public IActionResult Health() {
            return View();
        }

        [Authorize]
        [AccountRequired]
        public IActionResult Cache() {
            return View();
        }

        public IActionResult Match(string gameID) {
            return View();
        }

        /// <summary>
        ///     action to download a replay file
        /// </summary>
        /// <param name="gameID">ID of the game to get the replay file of</param>
        /// <returns></returns>
        public async Task<IActionResult> DownloadMatch(string gameID) {
            BarMatch? match = await _MatchRepository.GetByID(gameID);
            if (match == null) {
                return NotFound();
            }

            string path = Path.Join(_Options.Value.ReplayLocation, match.FileName);

            FileStream fs = System.IO.File.OpenRead(path);
            return File(fs, "application/octet-stream", fileDownloadName: match.FileName, false);
        }

    }
}
