using gex.Models;
using gex.Models.Db;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match")]
    public class BarMatchApiController : ApiControllerBase {

        private readonly ILogger<BarMatchApiController> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchPlayerDb _PlayerDb;
        private readonly BarMatchChatMessageDb _ChatMessageDb;
        private readonly BarMatchSpectatorDb _SpectatorDb;

        public BarMatchApiController(ILogger<BarMatchApiController> logger,
            BarMatchRepository matchRepository, BarMatchAllyTeamDb allyTeamDb,
            BarMatchPlayerDb playerDb, BarMatchChatMessageDb chatMessageDb,
            BarMatchSpectatorDb spectatorDb) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _AllyTeamDb = allyTeamDb;
            _PlayerDb = playerDb;
            _ChatMessageDb = chatMessageDb;
            _SpectatorDb = spectatorDb;
        }

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
                match.Players = await _PlayerDb.GetByGameID(gameID);
            }

            if (includePlayers == true) {
                match.ChatMessages = await _ChatMessageDb.GetByGameID(gameID);
            }

            if (includeSpectators == true) {
                match.Spectators = await _SpectatorDb.GetByGameID(gameID);
            }

            return ApiOk(match);
        }

    }
}
