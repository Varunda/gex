using gex.Code.Constants;
using gex.Code.ExtensionMethods;
using gex.Commands;
using gex.Models.Db;
using gex.Services.Db.Match;
using gex.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class MatchCommand {

        private readonly ILogger<MatchCommand> _Logger;
        private readonly BarMatchDb _MatchDb;

        public MatchCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<MatchCommand>>();
            _MatchDb = services.GetRequiredService<BarMatchDb>();
        }

        public void FixWrongSkillValues() {
            _Logger.LogInformation($"updating wrong_skill_values for all bar matches");

            new Task(async () => {
                try {
                    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(5));
                    List<BarMatch> allMatches = await _MatchDb.GetAll(cts.Token);

                    _Logger.LogInformation($"loaded all matches [count={allMatches.Count}]");
                    foreach (BarMatch match in allMatches) {
                        if (match.WrongSkillValues == true) {
                            continue;
                        }

                        int teamCount = match.SpadsSettings.GetRequiredInt32("nbteams");
                        int teamSize = match.SpadsSettings.GetRequiredInt32("teamsize");

                        match.WrongSkillValues = match.Gamemode != BarGamemode.GetByPlayers(teamCount, teamSize);

                        if (match.WrongSkillValues == true) {
                            _Logger.LogTrace($"found match with bad skill values [id={match.ID}] "
                                + $"[gamemode={match.Gamemode}/{BarGamemode.GetName(match.Gamemode)}] "
                                + $"[teamCount={teamCount}] [teamSize={teamSize}]");

                            await _MatchDb.UpdateWrongSkillValues(match, CancellationToken.None);
                        }
                    }

                    _Logger.LogInformation($"done!");
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to fix wrong skill values");
                }
            }).Start();
        }

    }
}
