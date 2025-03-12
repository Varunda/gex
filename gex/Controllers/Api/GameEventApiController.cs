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

        public GameEventApiController(ILogger<GameEventApiController> logger,
            GameEventTeamStatsDb teamStatsDb, GameEventUnitCreatedDb unitCreatedDb,
            GameEventUnitKilledDb unitKilledDb, BarMatchRepository matchRepository) {

            _Logger = logger;

            _MatchRepository = matchRepository;
            _TeamStatsDb = teamStatsDb;
            _UnitCreatedDb = unitCreatedDb;
            _UnitKilledDb = unitKilledDb;
        }

        [HttpGet("{gameID}")]
        public async Task<ApiResponse<GameOutput>> GetEvents(string gameID,
            [FromQuery] bool includeUnitsKilled = false,
            [FromQuery] bool includeUnitsCreated = false,
            [FromQuery] bool includeTeamStats = false
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

            return ApiOk(output);
        }

    }
}
