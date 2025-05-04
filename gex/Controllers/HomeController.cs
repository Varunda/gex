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
using System.Threading;
using System;
using gex.Services.Db.Match;
using System.Collections.Generic;
using System.Linq;

namespace gex.Controllers {

    public class HomeController : Controller {

        private readonly ILogger<HomeController> _Logger;

        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly HttpUtilService _HttpUtil;
        private readonly IOptions<FileStorageOptions> _Options;

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;

		public HomeController(ILogger<HomeController> logger,
			IHttpContextAccessor httpContextAccessor, HttpUtilService httpUtil,
			IOptions<FileStorageOptions> options, BarMatchRepository matchRepository,
			BarMatchAllyTeamDb allyTeamDb, BarMatchPlayerRepository playerRepository) {

			_HttpContextAccessor = httpContextAccessor;
			_HttpUtil = httpUtil;
			_Logger = logger;
			_Options = options;
			_MatchRepository = matchRepository;
			_AllyTeamDb = allyTeamDb;
			_PlayerRepository = playerRepository;
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
							ogDesc = $"{(biggestTeam >= 4 ? "Large team" : "Small team")}: " + string.Join(" v ", allyTeams.Select(iter => iter.PlayerCount));
						}
					}
				}, cancel).WaitAsync(TimeSpan.FromSeconds(1), cancel);

				ViewBag.OgDescription = ogDesc;
			} catch (Exception) {
				_Logger.LogWarning($"failed to generate og:description within 1s [gameID={gameID}]");
			}

            return View();
        }

        public new IActionResult User(int userID) {
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

    }
}
