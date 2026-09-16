using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Repository.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public class BarMatchProcessorUtil {

        private readonly ILogger<BarMatchProcessorUtil> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMatchTeamRepository _TeamRepository;
        private readonly IBarMatchAiPlayerDb _AiPlayerDb;
        private readonly IBarMatchAllyTeamDb _AllyTeamDb;

        public BarMatchProcessorUtil(ILogger<BarMatchProcessorUtil> logger,
            BarMatchRepository matchRepository, BarMatchPlayerRepository playerRepository,
            BarMatchTeamRepository teamRepository, IBarMatchAiPlayerDb aiPlayerDb,
            IBarMatchAllyTeamDb allyTeamDb) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _PlayerRepository = playerRepository;
            _TeamRepository = teamRepository;
            _AiPlayerDb = aiPlayerDb;
            _AllyTeamDb = allyTeamDb;
        }

        public async Task Insert(BarMatch match, CancellationToken cancel) {
            _Logger.LogDebug($"adding new match to DB [gameID={match.ID}]");
            await _MatchRepository.Insert(match, cancel);

            foreach (BarMatchTeam team in match.Teams) {
                await _TeamRepository.Insert(team, cancel);
            }

            foreach (BarMatchPlayer player in match.Players) {
                await _PlayerRepository.Insert(player);
            }

            foreach (BarMatchAiPlayer ai in match.AiPlayers) {
                await _AiPlayerDb.Insert(ai, cancel);
            }

            foreach (BarMatchAllyTeam at in match.AllyTeams) {
                await _AllyTeamDb.Insert(at);
            }

            _Logger.LogInformation($"inserted match into DB [gameID={match.ID}]");
        }

    }
}
