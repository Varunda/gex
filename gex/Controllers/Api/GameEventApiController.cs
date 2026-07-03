using gex.Common.Models;
using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Services;
using gex.Services.Db;
using gex.Services.Db.Event;
using gex.Services.Repositories;
using gex.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/game-event")]
    public class GameEventApiController : ApiControllerBase {

        private readonly ILogger<GameEventApiController> _Logger;
        private readonly ICurrentAccount _CurrentUser;
        private readonly GameOutputRepository _GameOutputRepository;

        private readonly BarMatchRepository _MatchRepository;
        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;

        public GameEventApiController(ILogger<GameEventApiController> logger,
            BarMatchRepository matchRepository, UnitPositionFileStorage unitPositionStorage,
            MatchPoolRepository matchPoolRepository, MatchPoolEntryDb matchPoolEntryDb,
            ICurrentAccount currentUser, GameOutputRepository gameOutputRepository) {

            _Logger = logger;

            _MatchRepository = matchRepository;
            _MatchPoolRepository = matchPoolRepository;
            _MatchPoolEntryDb = matchPoolEntryDb;
            _CurrentUser = currentUser;
            _GameOutputRepository = gameOutputRepository;
        }

        /// <summary>
        ///     get events of a <see cref="BarMatch"/>. by default, this will return NO events, and query parameters
        ///     must be set to tell Gex what to include
        /// </summary>
        /// <remarks>
        ///     each part of <see cref="GameOutput"/> that is wanted must be declared so. this is so future events being added
        ///     are not needlessly populated when that data is not used
        /// </remarks>
        /// <param name="gameID">ID of the <see cref="BarMatch"/> to get the game events of</param>
        /// <param name="includeUnitsKilled">if <see cref="GameOutput.UnitsKilled"/> will be populated, defaults to false</param>
        /// <param name="includeUnitsCreated">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeTeamStats">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeUnitDefs">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeExtraStats">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeWindUpdates">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeCommanderPositionUpdates">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeFactoryUnitCreate">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeUnitsGiven">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeUnitsTaken">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeTransportLoads">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeTransportUnloads">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeTeamDiedEvents">if <see cref="GameOutput.TeamDiedEvents"/> will be populated, defaults to false</param>
        /// <param name="includeUnitResources">if <see cref="GameOutput.UnitResources"/> will be populated, defaults to false</param>
        /// <param name="includeUnitDamage">if <see cref="GameOutput.UnitDamage"/> will be populated, defaults to false</param>
        /// <param name="includeUnitPosition">if <see cref="GameOutput.UnitPosition"/> will be populated, defaults to false</param>
        /// <param name="cancel">cancel token</param>
        /// <response code="200">
        ///     the response will contain a <see cref="GameOutput"/>, that has the various fields
        ///     populated based on the parameters passed
        /// </response>
        /// <response code="204">
        ///     no <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> exists.
        ///     NOTE: this could mean that Gex has not processed the match yet, it might still exist
        /// </response>
        [HttpGet("{gameID}")]
        public async Task<ApiResponse<GameOutput>> GetEvents(string gameID,
            [FromQuery] bool includeUnitsKilled = false,
            [FromQuery] bool includeUnitsCreated = false,
            [FromQuery] bool includeTeamStats = false,
            [FromQuery] bool includeUnitDefs = false,
            [FromQuery] bool includeExtraStats = false,
            [FromQuery] bool includeWindUpdates = false,
            [FromQuery] bool includeCommanderPositionUpdates = false,
            [FromQuery] bool includeFactoryUnitCreate = false,
            [FromQuery] bool includeUnitsGiven = false,
            [FromQuery] bool includeUnitsTaken = false,
            [FromQuery] bool includeTransportLoads = false,
            [FromQuery] bool includeTransportUnloads = false,
            [FromQuery] bool includeTeamDiedEvents = false,
            [FromQuery] bool includeUnitResources = false,
            [FromQuery] bool includeUnitDamage = false,
            [FromQuery] bool includeUnitPosition = false,
            CancellationToken cancel = default
        ) {

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return ApiNoContent<GameOutput>();
            }

            List<MatchPoolEntry> poolEntries = await _MatchPoolEntryDb.GetByMatchID(gameID, cancel);
            if (poolEntries.Count > 0) {
                AppAccount? currentUser = await _CurrentUser.Get(cancel);

                bool canView = false;
                foreach (MatchPoolEntry entry in poolEntries) {
                    canView |= await _MatchPoolRepository.CanView(entry.PoolID, currentUser?.ID, cancel);
                    if (canView == true) {
                        break;
                    }
                }

                if (canView == false) {
                    return ApiForbidden<GameOutput>($"this {nameof(BarMatch)} is in a match pool that is hidden");
                }
            }

            Result<GameOutput?, string> result = await _GameOutputRepository.Build(gameID, new GameOutputRepository.BuildOptions() {
                IncludeUnitsKilled = includeUnitsKilled,
                IncludeUnitsCreated = includeUnitsCreated,
                IncludeTeamStats = includeTeamStats,
                IncludeUnitDefs = includeUnitDefs,
                IncludeExtraStats = includeExtraStats,
                IncludeWindUpdates = includeWindUpdates,
                IncludeCommanderPositionUpdates = includeCommanderPositionUpdates,
                IncludeFactoryUnitCreate = includeFactoryUnitCreate,
                IncludeUnitsGiven = includeUnitsGiven,
                IncludeUnitsTaken = includeUnitsTaken,
                IncludeTransportLoads = includeTransportLoads,
                IncludeTransportUnloads = includeTransportUnloads,
                IncludeTeamDiedEvents = includeTeamDiedEvents,
                IncludeUnitResources = includeUnitResources,
                IncludeUnitDamage = includeUnitDamage,
                IncludeUnitPosition = includeUnitPosition
            }, await _CurrentUser.Get(cancel), cancel);

            if (result.IsOk == false) {
                return ApiInternalError<GameOutput>($"error getting game output: {result.Error}");
            }

            if (result.Value == null) {
                return ApiNoContent<GameOutput>();
            }

            return ApiOk(result.Value);
        }

    }
}
