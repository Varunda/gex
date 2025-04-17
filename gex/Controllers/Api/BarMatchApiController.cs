﻿using gex.Models;
using gex.Models.Api;
using gex.Models.Db;
using gex.Services.Db.Match;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match")]
    public class BarMatchApiController : ApiControllerBase {

        private readonly ILogger<BarMatchApiController> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMapRepository _BarMapRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchChatMessageDb _ChatMessageDb;
        private readonly BarMatchSpectatorDb _SpectatorDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMatchProcessingRepository _ProcessingRepository;

        public BarMatchApiController(ILogger<BarMatchApiController> logger,
            BarMatchRepository matchRepository, BarMatchAllyTeamDb allyTeamDb,
            BarMatchChatMessageDb chatMessageDb, BarMatchSpectatorDb spectatorDb,
            BarMatchPlayerRepository playerRepository, BarMapRepository barMapRepository,
            BarMatchProcessingRepository processingRepository) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _BarMapRepository = barMapRepository;
            _AllyTeamDb = allyTeamDb;
            _ChatMessageDb = chatMessageDb;
            _SpectatorDb = spectatorDb;
            _PlayerRepository = playerRepository;
            _ProcessingRepository = processingRepository;
        }

        /// <summary>
        ///     get a <see cref="BarMatch"/>, optionally including additional information
        /// </summary>
        /// <param name="cancel">cancel token</param>
        /// <param name="gameID">ID of the game</param>
        /// <param name="includeAllyTeams">will <see cref="BarMatch.AllyTeams"/> be populated? defaults to false</param>
        /// <param name="includePlayers">will <see cref="BarMatch.Players"/> be populated? defaults to false</param>
        /// <param name="includeChat">will <see cref="BarMatch.ChatMessages"/> be populated? defaults to false</param>
        /// <param name="includeSpectators">will <see cref="BarMatch.Spectators"/> be populated? defaults to false</param>
        /// <returns></returns>
        [HttpGet("{gameID}")]
        public async Task<ApiResponse<ApiMatch>> GetMatch(CancellationToken cancel,
            string gameID,
            [FromQuery] bool includeAllyTeams = false,
            [FromQuery] bool includePlayers = false,
            [FromQuery] bool includeChat = false,
            [FromQuery] bool includeSpectators = false
        ) {
            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return ApiNoContent<ApiMatch>();
            }

            if (includeAllyTeams == true) {
                match.AllyTeams = await _AllyTeamDb.GetByGameID(gameID, cancel);
            }

            if (includePlayers == true) {
                match.Players = await _PlayerRepository.GetByGameID(gameID, cancel);
            }

            if (includeChat == true) {
                match.ChatMessages = await _ChatMessageDb.GetByGameID(gameID, cancel);
            }

            if (includeSpectators == true) {
                match.Spectators = await _SpectatorDb.GetByGameID(gameID, cancel);
            }

            ApiMatch ret = new(match);
            ret.MapData = await _BarMapRepository.GetByName(match.MapName, cancel);
            ret.Processing = await _ProcessingRepository.GetByGameID(gameID, cancel);

            return ApiOk(ret);
        }

        /// <summary>
        ///     get recent matches that gex is aware of
        /// </summary>
        /// <param name="cancel">cancel token</param>
        /// <param name="offset">offset into the recent page. this is not a page offset, but a numerical offset</param>
        /// <param name="limit">limit of how many entries to return. must be between 0 and 100</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarMatch"/> ordered by <see cref="BarMatch.StartTime"/>
        /// </response>
        [HttpGet("recent")]
        public async Task<ApiResponse<List<ApiMatch>>> GetRecent(CancellationToken cancel,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 24
        ) {

            if (offset < 0) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(offset)} cannot be less than 0 (is {offset})");
            }
            if (limit <= 0 || limit > 100) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(limit)} must be between 0 and 100 (is {limit})");
            }

            List<ApiMatch> ret = [];
            List<BarMatch> matches = await _MatchRepository.GetRecent(offset, limit, cancel);
            foreach (BarMatch m in matches) {
                m.Players = await _PlayerRepository.GetByGameID(m.ID, cancel);
                m.AllyTeams = await _AllyTeamDb.GetByGameID(m.ID, cancel);

				ApiMatch api = new(m);
				api.Processing = await _ProcessingRepository.GetByGameID(m.ID, cancel);

                ret.Add(api);
            }

            return ApiOk(ret);
        }

		/// <summary>
		///		perform a search across all matches
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="gameVersion"></param>
		/// <param name="map"></param>
		/// <param name="startTimeAfter"></param>
		/// <param name="startTimeBefore"></param>
		/// <param name="durationMinimum"></param>
		/// <param name="durationMaximum"></param>
		/// <param name="ranked"></param>
		/// <param name="gamemode"></param>
		/// <param name="processingDownloaded"></param>
		/// <param name="processingParsed"></param>
		/// <param name="processingReplayed"></param>
		/// <param name="processingAction"></param>
		/// <param name="offset"></param>
		/// <param name="limit"></param>
		/// <param name="cancel"></param>
		/// <returns></returns>
		[HttpGet("search")]
		public async Task<ApiResponse<List<ApiMatch>>> Search(
			[FromQuery] string? engine = null,
			[FromQuery] string? gameVersion = null,
			[FromQuery] string? map = null,
			[FromQuery] DateTime? startTimeAfter = null,
			[FromQuery] DateTime? startTimeBefore = null,
			[FromQuery] long? durationMinimum = null,
			[FromQuery] long? durationMaximum = null,
			[FromQuery] bool? ranked = null,
			[FromQuery] byte? gamemode = null,
			[FromQuery] bool? processingDownloaded = null,
			[FromQuery] bool? processingParsed = null,
			[FromQuery] bool? processingReplayed = null,
			[FromQuery] bool? processingAction = null,

			[FromQuery] int offset = 0,
			[FromQuery] int limit = 24,

			CancellationToken cancel = default
		) {

            if (offset < 0) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(offset)} cannot be less than 0 (is {offset})");
            }
            if (limit <= 0 || limit > 100) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(limit)} must be between 0 and 100 (is {limit})");
            }

			BarMatchSearchParameters parms = new();
			parms.EngineVersion = engine;
			parms.GameVersion = gameVersion;
			parms.Map = map;
			parms.StartTimeAfter = startTimeAfter;
			parms.StartTimeBefore = startTimeBefore;
			parms.DurationMinimum = durationMinimum;
			parms.DurationMaximum = durationMaximum;
			parms.Ranked = ranked;
			parms.Gamemode = gamemode;
			parms.ProcessingDownloaded = processingDownloaded;
			parms.ProcessingParsed = processingParsed;
			parms.ProcessingReplayed = processingReplayed;
			parms.ProcessingAction = processingAction;

            List<ApiMatch> ret = [];
            List<BarMatch> matches = await _MatchRepository.Search(parms, offset, limit, cancel);
            foreach (BarMatch m in matches) {
                m.Players = await _PlayerRepository.GetByGameID(m.ID, cancel);
                m.AllyTeams = await _AllyTeamDb.GetByGameID(m.ID, cancel);

				ApiMatch api = new(m);
				api.Processing = await _ProcessingRepository.GetByGameID(m.ID, cancel);

                ret.Add(api);
            }

            return ApiOk(ret);

		}

        /// <summary>
        ///     get the <see cref="BarMatch"/>s that a user has played in (not spectated!)
        /// </summary>
        /// <param name="cancel">cancelation token</param>
        /// <param name="userID">ID of the user</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarMatch"/>s that
        ///     have a <see cref="BarMatch.Players"/> with <see cref="BarMatchPlayer.UserID"/> of <paramref name="userID"/>
        /// </response>
        [HttpGet("user/{userID}")]
        public async Task<ApiResponse<List<ApiMatch>>> GetByUserID(CancellationToken cancel, int userID) {
            List<ApiMatch> ret = [];
            List<BarMatch> matches = await _MatchRepository.GetByUserID(userID, cancel);
            foreach (BarMatch m in matches) {
                m.Players = await _PlayerRepository.GetByGameID(m.ID, cancel);
                m.AllyTeams = await _AllyTeamDb.GetByGameID(m.ID, cancel);

                ret.Add(new ApiMatch(m));
            }

            return ApiOk(ret);
        }

		/// <summary>
		///		get a list of all <see cref="ApiMatch"/>s that are pending processing in some way
		/// </summary>
		/// <param name="cancel">cancellation token</param>
		/// <returns></returns>
		[HttpGet("pending")]
		public async Task<ApiResponse<List<ApiMatch>>> GetPending(CancellationToken cancel = default) {
            List<BarMatchProcessing> processing = await _ProcessingRepository.GetPending(cancel);

			List<ApiMatch> ret = [];
			foreach (BarMatchProcessing proc in processing) {
				BarMatch? match = await _MatchRepository.GetByID(proc.GameID, cancel);
				if (match == null) {
					continue;
				}

				ApiMatch api = new(match);
				api.Processing = proc;

				ret.Add(api);
			}

			return ApiOk(ret);
		}

    }
}
