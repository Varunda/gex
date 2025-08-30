using gex.Code;
using gex.Code.Constants;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Options;
using gex.Models.UserStats;
using gex.Services;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers {

    public class HomeController : Controller {

        private readonly ILogger<HomeController> _Logger;

        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly HttpUtilService _HttpUtil;
        private readonly IOptions<FileStorageOptions> _Options;

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMapRepository _MapRepository;
        private readonly BarUserRepository _UserRepository;

        public HomeController(ILogger<HomeController> logger,
            IHttpContextAccessor httpContextAccessor, HttpUtilService httpUtil,
            IOptions<FileStorageOptions> options, BarMatchRepository matchRepository,
            BarMatchAllyTeamDb allyTeamDb, BarMatchPlayerRepository playerRepository,
            BarMapRepository mapRepository, BarUserRepository userRepository) {

            _HttpContextAccessor = httpContextAccessor;
            _HttpUtil = httpUtil;
            _Logger = logger;
            _Options = options;
            _MatchRepository = matchRepository;
            _AllyTeamDb = allyTeamDb;
            _PlayerRepository = playerRepository;
            _MapRepository = mapRepository;
            _UserRepository = userRepository;
        }

        public IActionResult Index() {
            return View();
        }

        [Authorize]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public IActionResult AccountManagement() {
            return View();
        }

        public IActionResult Health() {
            return View();
        }

        [Authorize]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public IActionResult Cache() {
            return View();
        }

        public async Task<IActionResult> Match(string gameID, CancellationToken cancel) {
            try {
                string? ogDesc = null;
                await Task.Run(async () => {
                    BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);

                    if (match == null) {
                        return;
                    }

                    if (match.PlayerCount == 2) {
                        List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(gameID, cancel);
                        if (players.Count != 2) {
                            _Logger.LogWarning($"expected 2 players from a match with a player count of 2 [gameID={gameID}] [player.Count={players.Count}]");
                            return;
                        }

                        ogDesc = $"Duel: {players[0].Name} / {players[1].Name}";
                    } else {
                        List<BarMatchAllyTeam> allyTeams = await _AllyTeamDb.GetByGameID(gameID, cancel);
                        if (allyTeams.Count == 0) {
                            _Logger.LogWarning($"expected at least 1 ally team [gameID={gameID}]");
                            return;
                        }

                        int biggestTeam = allyTeams.Select(iter => iter.PlayerCount).Max();
                        // FFA
                        if (biggestTeam == 1) {
                            ogDesc = $"{allyTeams.Count}-way FFA";
                        } else {
                            ogDesc = $"{BarGamemode.GetName(match.Gamemode)}: " + string.Join(" v ", allyTeams.Select(iter => iter.PlayerCount));
                        }
                    }

                    ogDesc += $" on {match.Map}";
                }, cancel).WaitAsync(TimeSpan.FromSeconds(1), cancel);

                ViewBag.OgDescription = ogDesc;
            } catch (Exception) {
                _Logger.LogWarning($"failed to generate og:description within 1s [gameID={gameID}]");
            }

            return View();
        }

        public new async Task<IActionResult> User(int userID, CancellationToken cancel) {
            try {
                string? ogDesc = null;
                await Task.Run(async () => {
                    BarUser? user = await _UserRepository.GetByID(userID, cancel);

                    if (user == null) {
                        return;
                    }

                    ogDesc = $"View user stats for {user.Username}";
                }, cancel).WaitAsync(TimeSpan.FromSeconds(1), cancel);

                ViewBag.OgDescription = ogDesc;
            } catch (Exception) {
                _Logger.LogWarning($"failed to generate og:description for user within 1s [userID={userID}]");
            }

            return View();
        }

        public IActionResult Legal() {
            return View();
        }

        public IActionResult Users() {
            return View();
        }

        public IActionResult Faq() {
            return View();
        }

        [PermissionNeeded(AppPermission.GEX_MATCH_UPLOAD)]
        [Authorize]
        public IActionResult Upload() {
            return View();
        }

        [Authorize]
        public IActionResult Login() {
            return View("Index");
        }

        public async Task<IActionResult> Logout(string? returnUrl = null) {
            await HttpContext.SignOutAsync();

            if (returnUrl == null) {
                return RedirectToAction("Index", "Home");
            }

            return Redirect(returnUrl);
        }

        /// <summary>
        ///     action to download a replay file
        /// </summary>
        /// <param name="gameID">ID of the game to get the replay file of</param>
        /// <returns></returns>
        public async Task<IActionResult> DownloadMatch(string gameID) {
            BarMatch? match = await _MatchRepository.GetByID(gameID, CancellationToken.None);
            if (match == null) {
                return NotFound();
            }

            string path = Path.Join(_Options.Value.ReplayLocation, match.FileName);

            FileStream fs = System.IO.File.OpenRead(path);
            return File(fs, "application/octet-stream", fileDownloadName: match.FileName, false);
        }

        public async Task<IActionResult> Map(string filename, CancellationToken cancel) {
            try {
                string? ogDesc = null;

                await Task.Run(async () => {
                    BarMap? map = await _MapRepository.GetByFileName(filename, cancel);

                    if (map == null) {
                        return;
                    }

                    ogDesc = $"View map info for {map.Name} by {map.Author}";
                }, cancel).WaitAsync(TimeSpan.FromSeconds(1), cancel);

                ViewBag.OgDescription = ogDesc;
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to generate og:description for map [filename={filename}]");
            }

            return View();
        }

        public async Task<IActionResult> MapName(string mapName, CancellationToken cancel) {
            BarMap? map = await _MapRepository.GetByName(mapName, cancel);

            if (map == null) {
                return RedirectToAction("Maps", "Home");
            }

            return Redirect($"/map/{map.FileName}");
        }

        public IActionResult Maps() {
            return View();
        }

        public IActionResult Recent() {
            return View();
        }

    }
}
