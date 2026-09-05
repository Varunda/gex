using gex.Code;
using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repositories;
using gex.Common.Services.Repository;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Storage;
using gex.Common.Services.Util;
using gex.Models;
using gex.Models.Api;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Services;
using gex.Services.Db;
using gex.Services.Db.Account;
using gex.Services.Db.Match;
using gex.Services.Db.Patches;
using gex.Services.Migrations;
using gex.Services.Repositories;
using gex.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match")]
    public class BarMatchApiController : ApiControllerBase {

        private readonly ILogger<BarMatchApiController> _Logger;
        private readonly ICurrentAccount _CurrentUser;

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMapRepository _BarMapRepository;
        private readonly IBarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchTeamRepository _TeamRepository;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly IBarMatchProcessingPriorityDb _ProcessingPriorityDb;
        private readonly HeadlessRunStatusRepository _HeadlessRunStatusRepository;
        private readonly AppAccountDbStore _AccountDb;
        private readonly GameOutputStorage _GameOutputStorage;
        private readonly BarDemofileParser _DemofileParser;
        private readonly DemofileStorage _DemofileStorage;
        private readonly BadGameVersionRepository _BadGameVersionRepository;
        private readonly StartSpotDataRepository _StartSpotDataRepository;
        private readonly BarMatchPlayerStartSpotMigration _PlayerStartSpotMigration;
        private readonly IBarMatchBuilderUtil _MatchBuilder;

        public BarMatchApiController(ILogger<BarMatchApiController> logger,
            BarMatchRepository matchRepository, IBarMatchAllyTeamDb allyTeamDb,
            BarMatchPlayerRepository playerRepository, BarMapRepository barMapRepository,
            BarMatchProcessingRepository processingRepository, HeadlessRunStatusRepository headlessRunStatusRepository,
            AppAccountDbStore accountDb, IBarMatchProcessingPriorityDb processingPriorityDb,
            ICurrentAccount currentUser, GameOutputStorage gameOutputStorage,
            BarDemofileParser demofileParser, DemofileStorage demofileStorage, 
            BadGameVersionRepository badGameVersionRepository, StartSpotDataRepository startSpotDataRepository,
            BarMatchPlayerStartSpotMigration playerStartSpotMigration, BarMatchTeamRepository teamRepository,
            IBarMatchBuilderUtil matchBuilder) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _BarMapRepository = barMapRepository;
            _AllyTeamDb = allyTeamDb;
            _PlayerRepository = playerRepository;
            _ProcessingRepository = processingRepository;
            _HeadlessRunStatusRepository = headlessRunStatusRepository;
            _AccountDb = accountDb;
            _ProcessingPriorityDb = processingPriorityDb;
            _CurrentUser = currentUser;
            _GameOutputStorage = gameOutputStorage;
            _DemofileParser = demofileParser;
            _DemofileStorage = demofileStorage;
            _BadGameVersionRepository = badGameVersionRepository;
            _StartSpotDataRepository = startSpotDataRepository;
            _PlayerStartSpotMigration = playerStartSpotMigration;
            _TeamRepository = teamRepository;
            _MatchBuilder = matchBuilder;
        }

        /// <summary>
        ///     get a <see cref="BarMatch"/>, optionally including additional information.
        ///     see remarks for how hidden match pools are handled
        /// </summary>
        /// <remarks>
        ///     some match pools are marked as hidden until a specific time.
        ///     if the match is in a <see cref="MatchPool"/> that is current hidden* (see more below), 
        ///     then a 403 is returned. if the match is in multiple match pools, the match is allowed
        ///     if at least one of the match pools is not hidden
        /// </remarks>
        /// <param name="cancel">cancel token</param>
        /// <param name="gameID">ID of the game</param>
        /// <param name="includeTeams">will <see cref="BarMatch.Teams"/> be populated? defaults to false</param>
        /// <param name="includeAllyTeams">will <see cref="BarMatch.AllyTeams"/> be populated? defaults to false</param>
        /// <param name="includePlayers">will <see cref="BarMatch.Players"/> be populated? defaults to false</param>
        /// <param name="includeChat">will <see cref="BarMatch.ChatMessages"/> be populated? defaults to false</param>
        /// <param name="includeSpectators">will <see cref="BarMatch.Spectators"/> be populated? defaults to false</param>
        /// <param name="includeTeamDeaths">will <see cref="BarMatch.TeamDeaths"/> be populated? defaults to false</param>
        /// <param name="includePlayerLeaves">will <see cref="BarMatch.PlayerLeaves"/> be populated? defaults to false</param>
        /// <param name="includeMapDraws">will <see cref="BarMatch.MapDraws"/> be populated? defaults to false</param>
        /// <param name="includeLabeledPings">
        ///     will <see cref="BarMatch.MapDraws"/> be populated with pings that include a label? defaults to false.
        ///     if <paramref name="includeMapDraws"/> is <c>true</c>, then this parameter is ignored (as it is a subset of data)
        /// </param>
        /// <param name="includeCommands">will <see cref="BarMatch.Commands"/> be populated? defaults to false</param>
        /// <param name="includeSelfDCommands">
        ///     will <see cref="BarMatch.Commands"/> be populated with Self-D commands? defaults to false.
        ///     if <paramref name="includeCommands"/>> is <c>true</c>, then this parameter is ignored (as it is a subset of the data)
        /// </param>
        /// <param name="includeStartRegionData">will <see cref="BarMatch.StartRegionData"/> be populated? defaults to false</param>
        /// <response code="200">
        ///     the reponse will contain the <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/>,
        ///     populating any of the fields with the parameters
        /// </response>
        /// <response code="204">
        ///     no <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> exist
        /// </response>
        /// <response code="403">
        ///     the user making the request lacks permission to view the <see cref="BarMatch"/>. this occurs
        ///     when the <see cref="BarMatch"/> is in a <see cref="MatchPool"/> that is currently hidden
        /// </response>
        [HttpGet("{gameID}")]
        public async Task<ApiResponse<ApiMatch>> GetMatch(string gameID,
            [FromQuery] bool includeTeams = false,
            [FromQuery] bool includeAllyTeams = false,
            [FromQuery] bool includePlayers = false,
            [FromQuery] bool includeChat = false,
            [FromQuery] bool includeSpectators = false,
            [FromQuery] bool includeTeamDeaths = false,
            [FromQuery] bool includePlayerLeaves = false,
            [FromQuery] bool includeMapDraws = false,
            [FromQuery] bool includeLabeledPings = false,
            [FromQuery] bool includeCommands = false,
            [FromQuery] bool includeSelfDCommands = false,
            [FromQuery] bool includeStartRegionData = false,
            CancellationToken cancel = default
        ) {
            Result<Maybe<BarMatch>, string> result = await _MatchBuilder.BuildMatch(gameID, new IBarMatchBuilderUtil.BuildOptions() {
                IncludeTeams = includeTeams,
                IncludeAllyTeams = includeAllyTeams,
                IncludePlayers = includePlayers,
                IncludeChat = includeChat,
                IncludeSpectators = includeSpectators,
                IncludeTeamDeaths = includeTeamDeaths,
                IncludePlayerLeaves = includePlayerLeaves,
                IncludeMapDraws = includeMapDraws,
                IncludeLabeledPings = includeLabeledPings,
                IncludeCommands = includeCommands,
                IncludeSelfDCommands = includeSelfDCommands,
                IncludeStartRegionData = includeStartRegionData,
            }, (await _CurrentUser.Get(cancel))?.ID, cancel);

            if (result.IsOk == false) {
                return ApiInternalError<ApiMatch>($"failed to build match");
            }

            if (result.Value.Has() == false) {
                return ApiNoContent<ApiMatch>();
            }

            BarMatch match = result.Value.Get();

            ApiMatch ret = new(match);
            ret.MapData = await _BarMapRepository.GetByFileName(match.MapName, cancel);
            if (ret.MapData != null && ret.StartSpotVersion != null) {
                ret.MapData.StartPositionData =
                    await _StartSpotDataRepository.GetByVersionAndMapFilename(ret.MapName, ret.StartSpotVersion.Value, cancel);
            }
            ret.Processing = await _ProcessingRepository.GetByGameID(gameID, cancel);
            ret.IsBadGameVersion = await _BadGameVersionRepository.IsBadGameVersion(match.GameVersion, cancel);

            // if the user looking at the match is not logged in, don't show the users who prioritized the game
            List<ulong> discordIds = await _ProcessingPriorityDb.GetByGameID(gameID, cancel);
            if (await _CurrentUser.Get(cancel) != null) {
                foreach (ulong discordId in discordIds) {
                    AppAccount? acc = await _AccountDb.GetByDiscordID(discordId, cancel);
                    if (acc != null) {
                        ret.UsersPrioritizing.Add(acc.Name);
                    }
                }
            } else {
                ret.UsersPrioritizing.AddRange(discordIds.Select(iter => ""));
            }

            ret.HeadlessRunStatus = _HeadlessRunStatusRepository.Get(gameID);
            if (ret.UploadedByID != null) {
                ret.UploadedBy = await _AccountDb.GetByID(ret.UploadedByID.Value, cancel);
            }

            return ApiOk(ret);
        }

        /// <summary>
        ///     get the stdout of a simulated game. account must have the dev permission
        /// </summary>
        /// <param name="gameID">ID of the <see cref="BarMatch"/> to get the stdout of</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the string that is the stdout of the game being replayed
        /// </response>
        /// <response code="400">
        ///     one of the following conditions was met:
        ///     <ul>
        ///         <li>the <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> was not locally replayed</li>
        ///         <li>there was an error getting the stdout of the game</li>
        ///     </ul>
        /// </response>
        /// <response code="404">
        ///     no <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> exists
        /// </response>
        [HttpGet("{gameID}/stdout")]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        public async Task<ApiResponse<string>> GetStdout(string gameID, CancellationToken cancel) {
            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                return ApiNotFound<string>($"{nameof(BarMatch)} {gameID}");
            }

            if (proc.ReplaySimulated == null) {
                return ApiBadRequest<string>($"{nameof(BarMatch)} {gameID} has not been locally simulated");
            }

            Result<string, string> stdout = await _GameOutputStorage.GetStdout(gameID, cancel);
            if (stdout.IsOk == false) {
                return ApiBadRequest<string>($"error getting stdout: {stdout.Error}");
            }

            return ApiOk(stdout.Value);
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

            return await Search(
                offset: offset, limit: limit,
                orderBy: "start_time", orderByDir: "desc",
                cancel: cancel
            );
        }

        /// <summary>
        ///		perform a search across all matches. if any parameters is null, it is not considered in the search
        /// </summary>
        /// <param name="engine">string containing the engine version</param>
        /// <param name="gameVersion">game version</param>
        /// <param name="map">map name, not the map filename</param>
        /// <param name="startTimeAfter">when the match started, exclusive</param>
        /// <param name="startTimeBefore">when the match started, inclusive</param>
        /// <param name="durationMinimum">duration in milliseconds, exclusive</param>
        /// <param name="durationMaximum">duration in milliseconds, inclusive</param>
        /// <param name="ranked">boolean, is the match ranked or not</param>
        /// <param name="gamemode">number indicating the gamemode. 1 = duel, 2 = small teams, 3 = large teams, 4 = ffa, 5 = team ffa, 0 = unknown</param>
        /// <param name="processingDownloaded">if the download has been done on this match</param>
        /// <param name="processingParsed">if the parsing has been done on this match</param>
        /// <param name="processingReplayed">if this match has been locally replayed</param>
        /// <param name="processingAction">if the action log has been parsed and inserted into the DB</param>
        /// <param name="playerCountMinimum">if not null, how many players minimum in the match (not including spectators)</param>
        /// <param name="playerCountMaximum">if not null, how many players maximum in the match (not including spectators)</param>
        /// <param name="legionEnabled">if set to true/false, will limit results to matches that have legion enabled or disabled</param>
        /// <param name="poolID">ID of the <see cref="MatchPool"/> to search for</param>
        /// <param name="gameSettings">
        ///     game settings to limit the results to. the key, value and operation are comma seperated.
        ///     <br/>
        ///     for example, <code>techsplit,1,eq</code> would return all matches with <see cref="BarMatch.GameSettings"/>.techsplit = '1'.
        ///     <br/>
        ///     the valid operations are: eq (equals), ne (not equals), st (starts with), en (ends with), and in (contains)
        /// </param>
        /// <param name="userIDs">list of user IDs to include. leave blank for any user</param>
        /// <param name="players">
        ///     advanced player filters. filters based on all provided parameters,
        ///     for example could be used to find air players in matches over 30 OS. 
        ///     pass a URL encoded JSON
        /// </param>
        /// <param name="minimumOS">minimum OS of all players in the match, exclusive</param>
        /// <param name="maximumOS">maximum OS of all players in the match, inclusive</param>
        /// <param name="minimumAverageOS">minimum average OS of all players in the match, exclusive</param>
        /// <param name="maximumAverageOS">maximum average OS of all players in the match, exclusive</param>
        /// <param name="replayedAfter">
        ///     shows games that were replayed after this time (inclusive).
        ///     implies <paramref name="processingReplayed"/> of true
        /// </param>
        /// <param name="replayedBefore">
        ///     shows games that were replayed before this time (exclusive).
        ///     implies <paramref name="processingReplayed"/> of true
        /// </param>
        /// <param name="offset">offset into the results. is a value, not a page number</param>
        /// <param name="limit">how many results to return. capped at 100</param>
        /// <param name="orderBy">field to order by. can only be: duration, player_count or start_time</param>
        /// <param name="orderByDir">how to order the results. can only be: asc, desc</param>
        /// <param name="cancel"></param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="ApiMatch"/>s that meet the conditions set in the parameters.
        ///     any parameter set to null or excluded (which would default to null) is not used
        /// </response>
        /// <response code="400">
        ///     one of the following conditions was not met:
        ///     <ul>
        ///         <li><paramref name="offset"/> was less than 0</li>
        ///         <li><paramref name="limit"/> was not between 1 and 100</li>
        ///         <li><paramref name="orderBy"/> was an invalid value</li>
        ///         <li><paramref name="orderByDir"/> was an invalid value</li>
        ///         <li><paramref name="gameSettings"/> was not formatted correctly</li>
        ///     </ul>
        /// </response>
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
            [FromQuery] int? playerCountMinimum = null,
            [FromQuery] int? playerCountMaximum = null,
            [FromQuery] bool? legionEnabled = null,
            [FromQuery] long? poolID = null,
            [FromQuery] List<string>? gameSettings = null,
            [FromQuery] List<long>? userIDs = null,
            [FromQuery] List<SearchPlayer>? players = null,
            [FromQuery] double? minimumOS = null,
            [FromQuery] double? maximumOS = null,
            [FromQuery] double? minimumAverageOS = null,
            [FromQuery] double? maximumAverageOS = null,
            [FromQuery] DateTime? replayedAfter = null,
            [FromQuery] DateTime? replayedBefore = null,

            [FromQuery] int offset = 0,
            [FromQuery] int limit = 24,
            [FromQuery] string orderBy = "start_time",
            [FromQuery] string orderByDir = "desc",

            CancellationToken cancel = default
        ) {

            if (offset < 0) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(offset)} cannot be less than 0 (is {offset})");
            }
            if (limit <= 0 || limit > 100) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(limit)} must be between 0 and 100 (is {limit})");
            }

            if (string.IsNullOrEmpty(orderBy.Trim())) {
                orderBy = "start_time";
            }
            OrderBy? order = BarMatchSearchParameters.ParseOrderBy(orderBy);
            if (order == null) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(orderBy)} can only be 'start_time'|'player_count'|'duration'");
            }

            if (string.IsNullOrEmpty(orderByDir.Trim())) {
                orderByDir = "desc";
            }
            OrderByDirection? dir = BarMatchSearchParameters.ParseOrderByDirection(orderByDir);
            if (dir == null) {
                return ApiBadRequest<List<ApiMatch>>($"{nameof(orderByDir)} can only be 'asc'|'desc'");
            }

            if (gameSettings != null) {
                foreach (string i in gameSettings) {
                    string[] parts = i.Split(",");
                    if (parts.Length != 3) {
                        return ApiBadRequest<List<ApiMatch>>($"game setting '{i}' expected to have 3 commas, for key,value,operation");
                    }
                    if (parts[2] != "eq" && parts[2] != "ne" && parts[2] != "st" && parts[2] != "en" && parts[2] != "in") {
                        return ApiBadRequest<List<ApiMatch>>($"3rd part of '{i}' must be 'eq'|'ne'|'st'|'en'|'in', unexpected '{parts[2]}'");
                    }
                }
            }

            AppAccount? currentUser = await _CurrentUser.Get(cancel);

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
            parms.PlayerCountMinimum = playerCountMinimum;
            parms.PlayerCountMaximum = playerCountMaximum;
            parms.ProcessingDownloaded = processingDownloaded;
            parms.ProcessingParsed = processingParsed;
            parms.ProcessingReplayed = processingReplayed;
            parms.ProcessingAction = processingAction;
            parms.LegionEnabled = legionEnabled;
            parms.PoolID = poolID;

            parms.GameSettings = gameSettings?.Select(iter => {
                string[] parts = iter.Split(",");
                if (parts.Length != 3) {
                    throw new Exception($"validation failed above, expected {iter} to split into 3 parts based on comma");
                }
                return new MatchSearchKeyValue() {
                    Key = parts[0],
                    Value = parts[1],
                    Operation = parts[2]
                };
            }).ToList() ?? [];

            parms.Players = players ?? [];
            if (userIDs != null) {
                foreach (long userID in userIDs) {
                    parms.Players.Add(new SearchPlayer() {
                        UserID = userID,
                    });
                }
            }

            parms.MinimumOS = minimumOS;
            parms.MaximumOS = maximumOS;
            parms.MinimumAverageOS = minimumAverageOS;
            parms.MaximumAverageOS = maximumAverageOS;
            parms.ReplayedAfter = replayedAfter;
            parms.ReplayedBefore = replayedBefore;
            parms.OrderBy = order;
            parms.OrderByDirection = dir;

            List<ApiMatch> ret = [];
            List<BarMatch> matches = await _MatchRepository.Search(parms, offset, limit, currentUser?.ID, cancel);
            foreach (BarMatch m in matches) {
                m.Teams = await _TeamRepository.GetByGameID(m.ID, cancel);
                m.Players = await _PlayerRepository.GetByGameID(m.ID, cancel);
                m.AllyTeams = await _AllyTeamDb.GetByGameID(m.ID, cancel);

                ApiMatch api = new(m);
                api.Processing = await _ProcessingRepository.GetByGameID(m.ID, cancel);
                api.IsBadGameVersion = await _BadGameVersionRepository.IsBadGameVersion(m.GameVersion, cancel);

                ret.Add(api);
            }

            return ApiOk(ret);
        }

        /// <summary>
        ///     count (up to 1k) how many matches match a filter
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
        /// <param name="playerCountMinimum"></param>
        /// <param name="playerCountMaximum"></param>
        /// <param name="legionEnabled"></param>
        /// <param name="poolID"></param>
        /// <param name="gameSettings"></param>
        /// <param name="userIDs"></param>
        /// <param name="players"></param>
        /// <param name="minimumOS"></param>
        /// <param name="maximumOS"></param>
        /// <param name="minimumAverageOS"></param>
        /// <param name="maximumAverageOS"></param>
        /// <param name="replayedAfter"></param>
        /// <param name="replayedBefore"></param>
        /// <param name="offset"></param>
        /// <param name="orderBy"></param>
        /// <param name="orderByDir"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("count")]
        public async Task<ApiResponse<int>> Count(
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
            [FromQuery] int? playerCountMinimum = null,
            [FromQuery] int? playerCountMaximum = null,
            [FromQuery] bool? legionEnabled = null,
            [FromQuery] long? poolID = null,
            [FromQuery] List<string>? gameSettings = null,
            [FromQuery] List<long>? userIDs = null,
            [FromQuery] List<SearchPlayer>? players = null,
            [FromQuery] double? minimumOS = null,
            [FromQuery] double? maximumOS = null,
            [FromQuery] double? minimumAverageOS = null,
            [FromQuery] double? maximumAverageOS = null,
            [FromQuery] DateTime? replayedAfter = null,
            [FromQuery] DateTime? replayedBefore = null,

            [FromQuery] int offset = 0,
            [FromQuery] string orderBy = "start_time",
            [FromQuery] string orderByDir = "desc",

            CancellationToken cancel = default
        ) {

            if (offset < 0) {
                return ApiBadRequest<int>($"{nameof(offset)} cannot be less than 0 (is {offset})");
            }

            if (string.IsNullOrEmpty(orderBy.Trim())) {
                orderBy = "start_time";
            }
            OrderBy? order = BarMatchSearchParameters.ParseOrderBy(orderBy);
            if (order == null) {
                return ApiBadRequest<int>($"{nameof(orderBy)} can only be 'start_time'|'player_count'|'duration'");
            }

            if (string.IsNullOrEmpty(orderByDir.Trim())) {
                orderByDir = "desc";
            }
            OrderByDirection? dir = BarMatchSearchParameters.ParseOrderByDirection(orderByDir);
            if (dir == null) {
                return ApiBadRequest<int>($"{nameof(orderByDir)} can only be 'asc'|'desc'");
            }

            if (gameSettings != null) {
                foreach (string i in gameSettings) {
                    string[] parts = i.Split(",");
                    if (parts.Length != 3) {
                        return ApiBadRequest<int>($"game setting '{i}' expected to have 3 commas, for key,value,operation");
                    }
                    if (parts[2] != "eq" && parts[2] != "ne" && parts[2] != "st" && parts[2] != "en" && parts[2] != "in") {
                        return ApiBadRequest<int>($"3rd part of '{i}' must be 'eq'|'ne'|'st'|'en'|'in', unexpected '{parts[2]}'");
                    }
                }
            }

            AppAccount? currentUser = await _CurrentUser.Get(cancel);

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
            parms.PlayerCountMinimum = playerCountMinimum;
            parms.PlayerCountMaximum = playerCountMaximum;
            parms.ProcessingDownloaded = processingDownloaded;
            parms.ProcessingParsed = processingParsed;
            parms.ProcessingReplayed = processingReplayed;
            parms.ProcessingAction = processingAction;
            parms.LegionEnabled = legionEnabled;
            parms.PoolID = poolID;

            parms.GameSettings = gameSettings?.Select(iter => {
                string[] parts = iter.Split(",");
                if (parts.Length != 3) {
                    throw new Exception($"validation failed above, expected {iter} to split into 3 parts based on comma");
                }
                return new MatchSearchKeyValue() {
                    Key = parts[0],
                    Value = parts[1],
                    Operation = parts[2]
                };
            }).ToList() ?? [];

            parms.Players = players ?? [];
            if (userIDs != null) {
                foreach (long userID in userIDs) {
                    parms.Players.Add(new SearchPlayer() {
                        UserID = userID,
                    });
                }
            }

            parms.MinimumOS = minimumOS;
            parms.MaximumOS = maximumOS;
            parms.MinimumAverageOS = minimumAverageOS;
            parms.MaximumAverageOS = maximumAverageOS;
            parms.ReplayedAfter = replayedAfter;
            parms.ReplayedBefore = replayedBefore;
            parms.OrderBy = order;
            parms.OrderByDirection = dir;

            List<BarMatch> matches = await _MatchRepository.Search(parms, offset, 1001, currentUser?.ID, cancel);

            return ApiOk(matches.Count);
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

            AppAccount? currentUser = await _CurrentUser.Get(cancel);
            BarMatchSearchParameters searchParameters = new() {
                Players = [ new SearchPlayer() { UserID = userID } ]
            };

            List<BarMatch> matches = [];
            int offset = 0;
            while (true) {
                List<BarMatch> iter = await _MatchRepository.Search(searchParameters, offset, 1000, currentUser?.ID, cancel);

                matches.AddRange(iter);
                offset += 1000;

                if (offset > 10000 || iter.Count < 1000) {
                    break;
                }
            }

            List<ApiMatch> ret = [];
            foreach (BarMatch m in matches) {
                m.Teams = await _TeamRepository.GetByGameID(m.ID, cancel);
                m.Players = await _PlayerRepository.GetByGameID(m.ID, cancel);
                m.AllyTeams = await _AllyTeamDb.GetByGameID(m.ID, cancel);

                ApiMatch match = new(m);
                match.Processing = await _ProcessingRepository.GetByGameID(m.ID, cancel);

                ret.Add(match);
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

        /// <summary>
        ///     recalculate the player start spots for a match. if there is no start spot data,
        ///     this does not generate it
        /// </summary>
        /// <param name="gameID">ID of the match to recalculate the player start spots for</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the match with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> had its
        ///     player start spot data recalculated
        /// </response>
        /// <response code="400">
        ///     the <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/>
        ///     has no <see cref="BarMatch.StartSpotVersion"/>
        /// </response>
        [HttpPost("{gameID}/recalculate-player-start-spots")]
        [Authorize]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        public async Task<ApiResponse> RecalculatePlayerStartSpots(string gameID, CancellationToken cancel = default) {

            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new InvalidOperationException($"how is current user null");

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return ApiNotFound($"{nameof(BarMatch)} {gameID}");
            }

            if (match.StartSpotVersion == null) {
                return ApiBadRequest($"{nameof(BarMatch)} {gameID} has no start spot version");
            }

            StartSpotData? data = await _StartSpotDataRepository.GetByVersionAndMapFilename(match.MapName, match.StartSpotVersion.Value, cancel);
            if (data == null) {
                return ApiInternalError($"no {nameof(StartSpotData)} for map {match.MapName} and version {match.StartSpotVersion} exists!");
            }

            Result<Maybe<BarMatch>, string> ret = await _MatchBuilder.BuildMatch(match.ID, new IBarMatchBuilderUtil.BuildOptions() {
                IncludeAllyTeams = true,
                IncludePlayers = true,
                IncludeTeams = true
            }, currentUser.ID, cancel);

            if (ret.IsOk == false) {
                return ApiInternalError($"failed to build match: {ret.Error}");
            }

            if (ret.Value.Has() == false) {
                throw new InvalidOperationException($"match is not supposed to be null here");
            }

            _Logger.LogInformation($"recalculating player start spots for match [gameID={match.ID}] [map={match.MapName}] [version={data.Version}]");
            await _PlayerStartSpotMigration.FixMatch(ret.Value.Get(), data, cancel);

            return ApiOk();
        }

        private async Task<Result<BarMatch, string>> _Parse(BarMatch match, DemofileParserOptions options, CancellationToken cancel) {
            Result<byte[], string> demofile = await _DemofileStorage.GetDemofileByFilename(match.FileName, cancel);
            if (demofile.IsOk == false) {
                _Logger.LogError($"failed to load demofile from storage [error={demofile.Error}] [filename={match.FileName}]");
                return demofile.Error;
            }

            Result<BarMatch, string> fromDemofile = await _DemofileParser.Parse(match.FileName, demofile.Value, options, cancel);
            if (fromDemofile.IsOk == false) {
                _Logger.LogError($"failed to parse demofile [error={demofile.Error}] [matchID={match.ID}]");
                return fromDemofile.Error;
            }

            return fromDemofile;
        }

    }
}
