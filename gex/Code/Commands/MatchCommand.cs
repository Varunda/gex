using gex.Common.Code.Constants;
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
using gex.Models.Demofile;
using gex.Services.Parser;
using gex.Services.Storage;
using gex.Common.Models;

namespace gex.Code.Commands {

    [Command]
    public class MatchCommand {

        private readonly ILogger<MatchCommand> _Logger;
        private readonly BarMatchDb _MatchDb;
        private readonly DemofileStorage _DemofileStorage;
        private readonly BarDemofileParser _DemofileParser;

        public MatchCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<MatchCommand>>();
            _MatchDb = services.GetRequiredService<BarMatchDb>();
            _DemofileStorage = services.GetRequiredService<DemofileStorage>();
            _DemofileParser = services.GetRequiredService<BarDemofileParser>();
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

                    _Logger.LogInformation($"done setting wrong skill value!");
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to fix wrong skill values");
                }
            }).Start();
        }

        public void AddStartOffset() {
            _Logger.LogInformation($"updating start_offset for all bar matches");

            new Task(async () => {
                try {
                    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(5));
                    List<BarMatch> allMatches = await _MatchDb.GetAll(cts.Token);

                    _Logger.LogInformation($"loaded all matches [count={allMatches.Count}]");
                    foreach (BarMatch match in allMatches) {
                        if (match.StartOffset != 0f) {
                            continue;
                        }

                        using CancellationTokenSource cts2 = new(TimeSpan.FromMinutes(1));

                        Result<byte[], string> bytes = await _DemofileStorage.GetDemofileByFilename(match.FileName, cts2.Token);
                        if (bytes.IsOk == false) {
                            _Logger.LogWarning($"failed to load data from demofile [id={match.ID}] [error={bytes.Error}]");
                            continue;
                        }

                        Result<BarMatch, string> parsed = await _DemofileParser.Parse(match.FileName, bytes.Value, new DemofileParserOptions(), cts2.Token);
                        if (parsed.IsOk == false) {
                            _Logger.LogWarning($"failed to parse demofile [id={match.ID}] [error={parsed.Error}]");
                            continue;
                        }

                        match.StartOffset = parsed.Value.StartOffset;
                        await _MatchDb.UpdateStartOffset(match, CancellationToken.None);
                        _Logger.LogDebug($"updated start offset for match [id={match.ID}] [offset={match.StartOffset}]");
                    }

                    _Logger.LogInformation($"done updating start offset!");
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to fix wrong skill values");
                }
            }).Start();
        }

    }
}
