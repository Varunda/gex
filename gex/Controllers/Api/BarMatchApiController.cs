﻿using gex.Models;
using gex.Models.Db;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match")]
    public class BarMatchApiController : ApiControllerBase {

        private readonly ILogger<BarMatchApiController> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchChatMessageDb _ChatMessageDb;
        private readonly BarMatchSpectatorDb _SpectatorDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;

        public BarMatchApiController(ILogger<BarMatchApiController> logger,
            BarMatchRepository matchRepository, BarMatchAllyTeamDb allyTeamDb,
            BarMatchChatMessageDb chatMessageDb, BarMatchSpectatorDb spectatorDb,
            BarMatchPlayerRepository playerRepository) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _AllyTeamDb = allyTeamDb;
            _ChatMessageDb = chatMessageDb;
            _SpectatorDb = spectatorDb;
            _PlayerRepository = playerRepository;
        }

        /// <summary>
        ///     get a <see cref="BarMatch"/>, optionally including additional information
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        /// <param name="includeAllyTeams">will <see cref="BarMatch.AllyTeams"/> be populated? defaults to false</param>
        /// <param name="includePlayers">will <see cref="BarMatch.Players"/> be populated? defaults to false</param>
        /// <param name="includeChat">will <see cref="BarMatch.ChatMessages"/> be populated? defaults to false</param>
        /// <param name="includeSpectators">will <see cref="BarMatch.Spectators"/> be populated? defaults to false</param>
        /// <returns></returns>
        [HttpGet("{gameID}")]
        public async Task<ApiResponse<BarMatch>> GetMatch(string gameID,
            [FromQuery] bool includeAllyTeams = false,
            [FromQuery] bool includePlayers = false,
            [FromQuery] bool includeChat = false,
            [FromQuery] bool includeSpectators = false
        ) {
            BarMatch? match = await _MatchRepository.GetByID(gameID);
            if (match == null) {
                return ApiNoContent<BarMatch>();
            }

            if (includeAllyTeams == true) {
                match.AllyTeams = await _AllyTeamDb.GetByGameID(gameID);
            }

            if (includePlayers == true) {
                match.Players = await _PlayerRepository.GetByGameID(gameID);
            }

            if (includeChat == true) {
                match.ChatMessages = await _ChatMessageDb.GetByGameID(gameID);
            }

            if (includeSpectators == true) {
                match.Spectators = await _SpectatorDb.GetByGameID(gameID);
            }

            return ApiOk(match);
        }

        /// <summary>
        ///     get recent matches that gex is aware of
        /// </summary>
        /// <param name="offset">offset into the recent page. this is not a page offset, but a numerical offset</param>
        /// <param name="limit">limit of how many entries to return. must be between 0 and 100</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarMatch"/> ordered by <see cref="BarMatch.StartTime"/>
        /// </response>
        [HttpGet("recent")]
        public async Task<ApiResponse<List<BarMatch>>> GetRecent(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 20
        ) {

            if (offset < 0) {
                return ApiBadRequest<List<BarMatch>>($"{nameof(offset)} cannot be less than 0 (is {offset})");
            }
            if (limit <= 0 || limit > 100) {
                return ApiBadRequest<List<BarMatch>>($"{nameof(limit)} must be between 0 and 100 (is {limit})");
            }

            List<BarMatch> matches = await _MatchRepository.GetRecent(offset, limit);
            foreach (BarMatch m in matches) {
                m.Players = await _PlayerRepository.GetByGameID(m.ID);
                m.AllyTeams = await _AllyTeamDb.GetByGameID(m.ID);
            }

            return ApiOk(matches);
        }

    }
}
