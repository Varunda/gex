using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Api;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.MapStats;
using gex.Models.Queues;
using gex.Models.UserStats;
using gex.Services.Db;
using gex.Services.Db.Event;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [Route("/api/user")]
    [ApiController]
    public class UserApiController : ApiControllerBase {

        private readonly ILogger<UserApiController> _Logger;
        private readonly BarUserRepository _UserRepository;
        private readonly BarUserSkillDb _SkillDb;
        private readonly BarUserMapStatsDb _MapStatsDb;
        private readonly BarUserFactionStatsDb _FactionStatsDb;
        private readonly BarMapDb _MapDb;
        private readonly MapStatsStartSpotRepository _StartSpotRepository;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchPlayerRepository _MatchPlayerRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BaseQueue<FixCountryCodeQueueEntry> _FixCountryCodeQueue;
        private readonly GameUnitsCreatedRepository _GameUnitsCreatedRepository;

        public UserApiController(ILogger<UserApiController> logger,
            BarUserRepository userRepository, BarUserMapStatsDb mapStatsDb,
            BarUserFactionStatsDb factionStatsDb, BarUserSkillDb skillDb,
            BarMapDb mapDb, MapStatsStartSpotRepository startSpotRepository,
            BarMatchPlayerRepository matchPlayerRepository, BarMatchRepository matchRepository,
            BarMatchAllyTeamDb allyTeamDb, BaseQueue<FixCountryCodeQueueEntry> fixCountryCodeQueue,
            GameUnitsCreatedRepository gameUnitsCreatedRepository) {

            _Logger = logger;
            _UserRepository = userRepository;
            _MapStatsDb = mapStatsDb;
            _FactionStatsDb = factionStatsDb;
            _SkillDb = skillDb;
            _MapDb = mapDb;
            _StartSpotRepository = startSpotRepository;
            _MatchPlayerRepository = matchPlayerRepository;
            _MatchRepository = matchRepository;
            _AllyTeamDb = allyTeamDb;
            _FixCountryCodeQueue = fixCountryCodeQueue;
            _GameUnitsCreatedRepository = gameUnitsCreatedRepository;
        }

        /// <summary>
        ///		get user info. if <see cref="ApiBarUser.Skill"/>, <see cref="ApiBarUser.MapStats"/> or <see cref="ApiBarUser.FactionStats"/>
        ///		is wanted, make sure to set the corresponding parameter to <c>true</c>
        /// </summary>
        /// <param name="userID">ID of the user to get</param>
        /// <param name="includeSkill">if <see cref="ApiBarUser.Skill"/> is populated or not. defaults to false</param>
        /// <param name="includeMapStats">if <see cref="ApiBarUser.MapStats"/> is populated or not. defaults to false</param>
        /// <param name="includeFactionStats">if <see cref="ApiBarUser.FactionStats"/> is populated or not. defaults to false</param>
        /// <param name="includePreviousNames">if <see cref="ApiBarUser.PreviousNames"/> will be populated or not. defaults to false</param>
        /// <param name="includeUnitsMade">if <see cref="ApiBarUser.UnitsMade"/> will be populated or not. defaults to false</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain the <see cref="ApiBarUser"/> with <see cref="ApiBarUser.UserID"/> of <paramref name="userID"/>
        /// </response>
        /// <response code="204">
        ///		no <see cref="ApiBarUser"/> with <see cref="ApiBarUser.UserID"/> of <paramref name="userID"/> exists
        /// </response>
        [HttpGet("{userID}")]
        public async Task<ApiResponse<ApiBarUser>> GetByUserID(long userID,
            [FromQuery] bool includeSkill = false,
            [FromQuery] bool includeMapStats = false,
            [FromQuery] bool includeFactionStats = false,
            [FromQuery] bool includePreviousNames = false,
            [FromQuery] bool includeUnitsMade = false,
            CancellationToken cancel = default
        ) {

            BarUser? user = await _UserRepository.GetByID(userID, cancel);
            if (user == null) {
                return ApiNoContent<ApiBarUser>();
            }

            if (user.CountryCode == null) {
                _FixCountryCodeQueue.Queue(new FixCountryCodeQueueEntry() {
                    UserID = user.UserID
                });
            }

            ApiBarUser response = new();
            response.UserID = user.UserID;
            response.Username = user.Username;
            response.CountryCode = user.CountryCode;

            if (includeSkill == true) {
                response.Skill = await _SkillDb.GetByUserID(userID, cancel);
            }

            if (includeMapStats == true) {
                response.MapStats = await _MapStatsDb.GetByUserID(userID, cancel);
            }

            if (includeFactionStats == true) {
                response.FactionStats = await _FactionStatsDb.GetByUserID(userID, cancel);
            }

            if (includePreviousNames == true) {
                response.PreviousNames = await _UserRepository.GetUserNames(userID, cancel);
            }

            if (includeUnitsMade == true) {
                response.UnitsMade = await _GameUnitsCreatedRepository.GetByUserID(userID, cancel);
            }

            return ApiOk(response);
        }

        /// <summary>
        ///     get a list of <see cref="ApiBarUser"/>s
        /// </summary>
        /// <param name="userIDs"></param>
        /// <param name="includeSkill"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("users")]
        public async Task<ApiResponse<List<ApiBarUser>>> GetByUserIDs([FromQuery] List<long> userIDs,
            [FromQuery] bool includeSkill = false,
            CancellationToken cancel = default) {

            if (userIDs.Count == 0) {
                return ApiBadRequest<List<ApiBarUser>>($"must provided at least one user ID in {nameof(userIDs)}");
            }

            if (userIDs.Count > 100) {
                return ApiBadRequest<List<ApiBarUser>>($"cannot provide more than 100 user IDs");
            }

            List<BarUser> users = await _UserRepository.GetByIDs(userIDs, cancel);

            List<ApiBarUser> ret = [];
            foreach (BarUser user in users) {
                if (user.CountryCode == null) {
                    _FixCountryCodeQueue.Queue(new FixCountryCodeQueueEntry() {
                        UserID = user.UserID
                    });
                }

                ApiBarUser api = new();
                api.UserID = user.UserID;
                api.Username = user.Username;
                api.CountryCode = user.CountryCode;

                if (includeSkill == true) {
                    api.Skill = await _SkillDb.GetByUserID(api.UserID, cancel);
                }

                ret.Add(api);
            }

            return ApiOk(ret);
        }

        /// <summary>
        ///     get the skill changes of a user over time
        /// </summary>
        /// <param name="userID">ID of the user to get the skill changes of</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a <see cref="BarUserSkillChanges"/>, that contains the skill changes for each gamemode
        /// </response>
        /// <response code="204">
        ///     no <see cref="BarUser"/> with <see cref="BarUser.UserID"/> of <paramref name="userID"/> exists
        /// </response>
        [HttpGet("{userID}/skill-changes")]
        public async Task<ApiResponse<BarUserSkillChanges>> GetUserSkillChanges(long userID, CancellationToken cancel) {
            BarUser? user = await _UserRepository.GetByID(userID, cancel);
            if (user == null) {
                return ApiNoContent<BarUserSkillChanges>();
            }

            List<BarMatchPlayer> players = await _MatchPlayerRepository.GetByUserID(userID, cancel);
            Dictionary<string, BarMatch> matches = (await _MatchRepository.GetByIDs(players.Select(iter => iter.GameID).Distinct(), cancel))
                .ToDictionaryDistinct(iter => iter.ID);

            BarUserSkillChanges changes = new();
            Dictionary<byte, BarUserSkillGamemode> gamemodes = [];

            foreach (BarMatchPlayer p in players) {
                BarMatch? match = matches.GetValueOrDefault(p.GameID);
                if (match == null) {
                    _Logger.LogWarning($"missing {nameof(BarMatch)} from {nameof(BarMatchPlayer)} [gameID={p.GameID}] [userID={p.UserID}]");
                    continue;
                }

                if (match.Gamemode == 0 || match.WrongSkillValues == true) {
                    continue;
                }

                BarUserSkillGamemode gm = gamemodes.GetValueOrDefault(match.Gamemode)
                    ?? new BarUserSkillGamemode() { Gamemode = match.Gamemode };

                gm.Changes.Add(new BarUserSkillChangeEntry() {
                    Skill = p.Skill,
                    SkillUncertainty = p.SkillUncertainty,
                    Timestamp = match.StartTime
                });

                gamemodes[match.Gamemode] = gm;
            }

            changes.UserID = userID;
            changes.Gamemodes = gamemodes.Values.ToList().Select(iter => { 
                iter.Changes = [.. iter.Changes.OrderBy(i2 => i2.Timestamp)];
                return iter;
            }).ToList();

            return ApiOk(changes);
        }

        /// <summary>
        ///     get the <see cref="ApiBarUserInteractions"/> of a <see cref="BarUser"/>
        /// </summary>
        /// <param name="userID"><see cref="BarUser.UserID"/> of the <see cref="BarUser"/> to get the interactions of</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="ApiBarUserInteractions"/> for the user
        /// </response>
        /// <response code="204">
        ///     no <see cref="BarUser"/> with <see cref="BarUser.UserID"/> of <paramref name="userID"/> exists
        /// </response>
        [HttpGet("{userID}/interactions")]
        public async Task<ApiResponse<List<ApiBarUserInteractions>>> GetUserInteractions(long userID, CancellationToken cancel = default) {
            BarUser? user = await _UserRepository.GetByID(userID, cancel);
            if (user == null) {
                return ApiNoContent<List<ApiBarUserInteractions>>();
            }

            List<BarMatchPlayer> players = await _MatchPlayerRepository.GetByUserID(userID, cancel);

            IEnumerable<string> gameIDs = players.Select(iter => iter.GameID).Distinct();

            // load all players for all games the player has played in
            Dictionary<string, List<BarMatchPlayer>> dict = [];
            List<BarMatchPlayer> allPlayers = await _MatchPlayerRepository.GetByGameIDs(gameIDs, cancel);
            foreach (BarMatchPlayer p in await _MatchPlayerRepository.GetByGameIDs(gameIDs, cancel)) {
                List<BarMatchPlayer> gamePlayers = dict.GetValueOrDefault(p.GameID) ?? [];
                gamePlayers.Add(p);
                dict[p.GameID] = gamePlayers;
            }

            // load all ally teams for the games the player has been in
            Dictionary<string, List<BarMatchAllyTeam>> allyTeamDict = [];
            List<BarMatchAllyTeam> allyTeams = await _AllyTeamDb.GetByGameIDs(gameIDs, cancel);
            foreach (BarMatchAllyTeam at in allyTeams) {
                List<BarMatchAllyTeam> gameAts = allyTeamDict.GetValueOrDefault(at.GameID) ?? [];
                gameAts.Add(at);
                allyTeamDict[at.GameID] = gameAts;
            }

            Dictionary<long, BarUserInteractions> ints = [];

            foreach (BarMatchPlayer p in players) {
                List<BarMatchPlayer>? gamePlayers = dict.GetValueOrDefault(p.GameID);
                if (gamePlayers == null) {
                    _Logger.LogWarning($"missing game players for interactions [gameID={p.GameID}] [userID={userID}]");
                    continue;
                }

                List<BarMatchAllyTeam>? gameAllyTeams = allyTeamDict.GetValueOrDefault(p.GameID);
                if (gameAllyTeams == null) {
                    _Logger.LogWarning($"missing game ally teams for interactions [gameID={p.GameID}] [userID={userID}]");
                    continue;
                }

                foreach (BarMatchPlayer gamePlayer in gamePlayers) {
                    if (gamePlayer.UserID == userID) { continue; }

                    BarUserInteractions inter = ints.GetValueOrDefault(gamePlayer.UserID) ?? new BarUserInteractions() {
                        UserID = userID,
                        TargetUserID = gamePlayer.UserID
                    };

                    BarMatchAllyTeam? at = gameAllyTeams.FirstOrDefault(iter => iter.AllyTeamID == gamePlayer.AllyTeamID);
                    if (at == null) {
                        _Logger.LogWarning($"missing specific ally team for a game for interactions "
                            + $"[gameID={p.GameID}] [allyTeamID={gamePlayer.AllyTeamID}] [userID={userID}]");
                        continue;
                    }
                    
                    if (p.AllyTeamID == gamePlayer.AllyTeamID) {
                        inter.WithCount += 1;
                        if (at.Won == true) {
                            inter.WithWin += 1;
                        }
                    } else {
                        inter.AgainstCount += 1;
                        if (at.Won == false) { // the ally team is for the target User ID, so if their team lost, that means the user's ally team won
                            inter.AgainstWin += 1;
                        }
                    }

                    ints[inter.TargetUserID] = inter;
                }
            }

            List<ApiBarUserInteractions> apiInters = [];
            foreach (KeyValuePair<long, BarUserInteractions> inter in ints) {
                apiInters.Add(new ApiBarUserInteractions() {
                    Interactions = inter.Value,
                    User = await _UserRepository.GetByID(inter.Value.TargetUserID, cancel)
                });
            }

            return ApiOk(apiInters);
        }

        /// <summary>
        ///		search users based on current username (case-insensitive).
        ///		<see cref="ApiBarUser.FactionStats"/> and <see cref="ApiBarUser.MapStats"/> is never populated
        /// </summary>
        /// <param name="search">text to search for. must be at least 3 non-wildcard characters long</param>
        /// <param name="searchPreviousNames">will previous names be searched against as well? defaults to false</param>
        /// <param name="includeSkill">if <see cref="ApiBarUser.Skill"/> will be populated. defaults to false</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain a list of <see cref="UserSearchResult"/>s that match <paramref name="search"/>,
        ///		and will include <see cref="UserSearchResult.Skill"/> if <paramref name="includeSkill"/> is true,
        ///		and if <paramref name="searchPreviousNames"/> is true, the <see cref="UserSearchResult.Username"/>
        ///		might not match <paramref name="search"/>, but <see cref="UserSearchResult.PreviousName"/> will
        /// </response>
        /// <response code="400">
        ///		<paramref name="search"/> is not at least 3 characters long
        /// </response>
        [HttpGet("search/{search}")]
        public async Task<ApiResponse<List<UserSearchResult>>> Search(string search,
            [FromQuery] bool includeSkill = false,
            [FromQuery] bool searchPreviousNames = false,
            CancellationToken cancel = default
        ) {

            // ignore wildcard when imposing character minimum
            if (search.Where(iter => iter != '*').Count() < 3) {
                return ApiBadRequest<List<UserSearchResult>>($"search must be at least 3 non-wildcard characters");
            }

            List<UserSearchResult> users = await _UserRepository.SearchByName(search, searchPreviousNames, cancel);

            foreach (UserSearchResult user in users) {
                if (includeSkill == true) {
                    user.Skill = await _SkillDb.GetByUserID(user.UserID, cancel);
                }
            }

            return ApiOk(users);
        }

        /// <summary>
        ///     get the start spots of a user on a specific map, either by map name or map filename
        /// </summary>
        /// <param name="userID">ID of the user</param>
        /// <param name="mapFilename">filename of the map. either this one or <paramref name="mapName"/> must be given</param>
        /// <param name="mapName">name of the map. either this or <paramref name="mapFilename"/> must be given</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="MapStatsStartSpot"/>s
        ///     for the user on the map
        /// </response>
        /// <response code="400">
        ///     neither <paramref name="mapFilename"/> or <paramref name="mapName"/> was given
        /// </response>
        /// <response code="404">
        ///     one of the following objects could not be found:
        ///     <ul>
        ///         <li>no <see cref="BarUser"/> with <see cref="BarUser.UserID"/> of <paramref name="userID"/> exists</li>
        ///         <li>no <see cref="BarMap"/> with <see cref="BarMap.FileName"/> of <paramref name="mapFilename"/> exists</li>
        ///     </ul>
        /// </response>
        [HttpGet("start-spots/{userID}")]
        public async Task<ApiResponse<List<MapStatsStartSpot>>> GetStartSpotsByName(
            long userID,
            [FromQuery] string? mapFilename = null,
            [FromQuery] string? mapName = null,
            CancellationToken cancel = default
        ) {

            if (mapFilename == null && mapName == null) {
                return ApiBadRequest<List<MapStatsStartSpot>>($"either {nameof(mapFilename)} or {nameof(mapName)} must be given");
            }
            if (mapFilename != null && mapName != null) {
                return ApiBadRequest<List<MapStatsStartSpot>>($"cannot give both {nameof(mapFilename)} and {nameof(mapName)}");
            }

            BarMap? map = null;

            if (mapFilename != null) {
                map = await _MapDb.GetByFileName(mapFilename, cancel);
                if (map == null) {
                    return ApiNotFound<List<MapStatsStartSpot>>($"{nameof(BarMap)} {mapFilename}");
                }
            }

            if (mapName != null) {
                map = await _MapDb.GetByName(mapName, cancel);
                if (map == null) {
                    return ApiNotFound<List<MapStatsStartSpot>>($"{nameof(BarMap)} {mapName}");
                }
            }

            if (map == null) {
                throw new System.Exception($"logic error: why is map null here, 404 was expected");
            }

            BarUser? user = await _UserRepository.GetByID(userID, cancel);
            if (user == null) {
                return ApiNotFound<List<MapStatsStartSpot>>($"{nameof(BarUser)} {userID}");
            }

            List<MapStatsStartSpot> spots = await _StartSpotRepository.GetByMapAndUser(map.FileName, userID, cancel);
            return ApiOk(spots);
        }

    }
}
