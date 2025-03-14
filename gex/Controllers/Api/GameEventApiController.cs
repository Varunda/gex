using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/game-event")]
    public class GameEventApiController : ApiControllerBase {

        private readonly ILogger<GameEventApiController> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly GameEventTeamStatsDb _TeamStatsDb;
        private readonly GameEventUnitCreatedDb _UnitCreatedDb;
        private readonly GameEventUnitKilledDb _UnitKilledDb;
        private readonly UnitSetToGameIdDb _GameToHashDb;
        private readonly GameEventUnitDefDb _UnitDefDb;
        private readonly GameEventArmyValueUpdateDb _ArmyUpdateDb;
        private readonly GameEventWindUpdateDb _WindUpdateDb;
        private readonly GameEventCommanderPositionUpdateDb _CommanderPositionDb;
        private readonly GameEventFactoryUnitCreatedDb _FactoryUnitCreatedDb;
        private readonly GameEventUnitGivenDb _UnitGivenDb;
        private readonly GameEventUnitTakenDb _UnitTakenDb;
        private readonly GameEventTransportLoadedDb _TransportLoadedDb;
        private readonly GameEventTransportUnloadedDb _TransportUnloadedDb;

        public GameEventApiController(ILogger<GameEventApiController> logger,
            GameEventTeamStatsDb teamStatsDb, GameEventUnitCreatedDb unitCreatedDb,
            GameEventUnitKilledDb unitKilledDb, BarMatchRepository matchRepository,
            GameEventUnitDefDb unitDefDb, UnitSetToGameIdDb gameToHashDb,
            GameEventArmyValueUpdateDb armyUpdateDb, GameEventWindUpdateDb windUpdateDb,
            GameEventCommanderPositionUpdateDb commanderPositionDb, GameEventFactoryUnitCreatedDb factoryUnitCreatedDb,
            GameEventUnitGivenDb unitGivenDb, GameEventUnitTakenDb unitTakenDb,
            GameEventTransportLoadedDb transportLoadedDb, GameEventTransportUnloadedDb transportUnloadedDb) {

            _Logger = logger;

            _MatchRepository = matchRepository;
            _TeamStatsDb = teamStatsDb;
            _UnitCreatedDb = unitCreatedDb;
            _UnitKilledDb = unitKilledDb;
            _UnitDefDb = unitDefDb;
            _GameToHashDb = gameToHashDb;
            _ArmyUpdateDb = armyUpdateDb;
            _WindUpdateDb = windUpdateDb;
            _CommanderPositionDb = commanderPositionDb;
            _FactoryUnitCreatedDb = factoryUnitCreatedDb;
            _UnitGivenDb = unitGivenDb;
            _UnitTakenDb = unitTakenDb;
            _TransportLoadedDb = transportLoadedDb;
            _TransportUnloadedDb = transportUnloadedDb;
        }

        /// <summary>
        ///     get events of a <see cref="BarMatch"/>
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
        /// <param name="includeArmyValueUpdates">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeWindUpdates">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeCommanderPositionUpdates">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeFactoryUnitCreate">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeUnitsGiven">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeUnitsTaken">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeTransportLoads">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
        /// <param name="includeTransportUnloads">if <see cref="GameOutput.UnitsCreated"/> will be populated, defaults to false</param>
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
            [FromQuery] bool includeArmyValueUpdates = false,
            [FromQuery] bool includeWindUpdates = false,
            [FromQuery] bool includeCommanderPositionUpdates = false,
            [FromQuery] bool includeFactoryUnitCreate = false,
            [FromQuery] bool includeUnitsGiven = false,
            [FromQuery] bool includeUnitsTaken = false,
            [FromQuery] bool includeTransportLoads = false,
            [FromQuery] bool includeTransportUnloads = false
        ) {

            BarMatch? match = await _MatchRepository.GetByID(gameID);
            if (match == null) {
                return ApiNoContent<GameOutput>();
            }

            GameOutput output = new();
            output.GameID = gameID;

            if (includeTeamStats == true) {
                output.TeamStats = await _TeamStatsDb.GetByGameID(gameID);
            }

            if (includeUnitsKilled == true) {
                output.UnitsKilled = await _UnitKilledDb.GetByGameID(gameID);
            }

            if (includeUnitsCreated == true) {
                output.UnitsCreated = await _UnitCreatedDb.GetByGameID(gameID);
            }

            if (includeArmyValueUpdates == true) {
                output.ArmyValueUpdates = await _ArmyUpdateDb.GetByGameID(gameID);
            }

            if (includeWindUpdates == true) {
                output.WindUpdates = await _WindUpdateDb.GetByGameID(gameID);
            }

            if (includeCommanderPositionUpdates == true) {
                output.CommanderPositionUpdates = await _CommanderPositionDb.GetByGameID(gameID);
            }

            if (includeFactoryUnitCreate == true) {
                output.FactoryUnitCreated = await _FactoryUnitCreatedDb.GetByGameID(gameID);
            }

            if (includeUnitsGiven == true) {
                output.UnitsGiven = await _UnitGivenDb.GetByGameID(gameID);
            }

            if (includeUnitsTaken == true) {
                output.UnitsTaken = await _UnitTakenDb.GetByGameID(gameID);
            }

            if (includeTransportLoads == true) {
                output.TransportLoaded = await _TransportLoadedDb.GetByGameID(gameID);
            }

            if (includeTransportUnloads == true) {
                output.TransportUnloaded = await _TransportUnloadedDb.GetByGameID(gameID);
            }

            if (includeUnitDefs == true) {
                GameIdToUnitDefHash? hash = await _GameToHashDb.GetByGameID(gameID);
                if (hash != null) {
                    output.UnitDefinitions = await _UnitDefDb.GetByHash(hash.Hash);
                } else {
                    _Logger.LogWarning($"game hash is not set! [gameID={gameID}]");
                }
            }

            return ApiOk(output);
        }

    }
}
