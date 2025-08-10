using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Services.Db.Match;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Util {

    public class BarMatchTitleUtilService {

        private readonly ILogger<BarMatchTitleUtilService> _Logger;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;

        public BarMatchTitleUtilService(ILogger<BarMatchTitleUtilService> logger,
            BarMatchPlayerRepository playerRepository, BarMatchAllyTeamDb allyTeamDb) {

            _Logger = logger;
            _PlayerRepository = playerRepository;
            _AllyTeamDb = allyTeamDb;
        }

        /// <summary>
        ///     get a string that is the title of a match used for display on Discord
        /// </summary>
        /// <param name="match"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<string> GetDiscordTitle(BarMatch match, CancellationToken cancel) {
            string title;
            if (match.PlayerCount == 2) {
                List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(match.ID, cancel);

                if (players.Count != 2) {
                    title = $"ERROR: expected 2 players, got {players.Count} instead";
                } else {
                    title = $"Duel: {players[0].Name} v {players[1].Name}";
                }
            } else {
                List<BarMatchAllyTeam> allyTeams = await _AllyTeamDb.GetByGameID(match.ID, cancel);
                if (allyTeams.Count == 0) {
                    title = $"ERROR: got 0 ally teams";
                } else {
                    int biggestTeam = allyTeams.Select(iter => iter.PlayerCount).Max();
                    // FFA
                    if (biggestTeam == 1) {
                        title = $"{allyTeams.Count}-way FFA";
                    } else {
                        title = $"{(biggestTeam >= 4 ? "Large team" : "Small team")}: " + string.Join(" v ", allyTeams.Select(iter => iter.PlayerCount));
                    }
                }
            }

            title = $"{match.StartTime.GetDiscordTimestamp("D")} - {title} on {match.Map}";

            return title;
        }

    }
}
