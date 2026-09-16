using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public class CovenBarMatchBuilderUtil : IBarMatchBuilderUtil {

        private readonly ILogger<CovenBarMatchBuilderUtil> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly IBarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchTeamRepository _TeamRepository;
        private readonly IBarMatchAiPlayerDb _AiPlayerDb;

        public CovenBarMatchBuilderUtil( ILogger<CovenBarMatchBuilderUtil> logger,
            BarMatchRepository matchRepository, BarMatchPlayerRepository playerRepository,
            IBarMatchAllyTeamDb allyTeamDb, BarMatchTeamRepository teamRepository,
            IBarMatchAiPlayerDb aiPlayerDb) {

            _MatchRepository = matchRepository;
            _PlayerRepository = playerRepository;
            _AllyTeamDb = allyTeamDb;
            _TeamRepository = teamRepository;
            _AiPlayerDb = aiPlayerDb;
            _Logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Result<Maybe<BarMatch>, string>> BuildMatch(string gameID, 
            IBarMatchBuilderUtil.BuildOptions options, long? _, CancellationToken cancel) {


            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return Maybe<BarMatch>.None();
            }

            if (options.IncludeAllyTeams == true) {
                match.AllyTeams = await _AllyTeamDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludePlayers == true) {
                match.Players = await _PlayerRepository.GetByGameID(gameID, cancel);
            }

            if (options.IncludeTeams == true) {
                match.Teams = await _TeamRepository.GetByGameID(gameID, cancel);
            }

            if (options.IncludeAiPlayers == true) {
                match.AiPlayers = await _AiPlayerDb.GetByGameID(gameID, cancel);
            }

            return Maybe<BarMatch>.Some(match);
        }

    }
}
