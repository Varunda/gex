using gex.Common.Models;
using gex.Models.Db;
using gex.Models.Demofile;
using gex.Models.Queues;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Migrations {

    public class BarMatchTeamPlayerMigration {

        private readonly ILogger<BarMatchTeamPlayerMigration> _Logger;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly DemofileStorage _DemofileStorage;
        private readonly BarMatchPlayerRepository _MatchPlayerRepository;
        private readonly BarMatchTeamRepository _MatchTeamRepository;
        private readonly BarDemofileParser _Parser;
        private readonly BarMatchRepository _MatchRepository;

        public BarMatchTeamPlayerMigration(ILogger<BarMatchTeamPlayerMigration> logger,
            BarMatchProcessingRepository processingRepository, BaseQueue<GameReplayParseQueueEntry> parseQueue,
            DemofileStorage demofileStorage, BarMatchPlayerRepository matchPlayerRepository,
            BarMatchTeamRepository matchTeamRepository, BarDemofileParser parser,
            BarMatchRepository matchRepository) {

            _Logger = logger;
            _ProcessingRepository = processingRepository;
            _ParseQueue = parseQueue;
            _DemofileStorage = demofileStorage;
            _MatchPlayerRepository = matchPlayerRepository;
            _MatchTeamRepository = matchTeamRepository;
            _Parser = parser;
            _MatchRepository = matchRepository;
        }

        public async Task FixAll(CancellationToken cancel) {

            List<BarMatchProcessing> pending = [];

            do {
                pending = await _ProcessingRepository.NeedsTeamsReparse(cancel);

                Stopwatch timer = Stopwatch.StartNew();
                foreach (BarMatchProcessing proc in pending) {
                    /*
                    _ParseQueue.Queue(new GameReplayParseQueueEntry() {
                        GameID = proc.GameID,
                        Force = true,
                        ForceForward = false,
                        SkipStatUpdates = true,
                        SkipWebhook = true,
                    });
                    */
                    await FixGame(proc.GameID, cancel);
                }

                _Logger.LogDebug($"completed batch [timer={timer.ElapsedMilliseconds}ms] [timePer={(pending.Count / (float)timer.ElapsedMilliseconds):F3}ms]");

                /*
                while (_ParseQueue.Count() > 0) {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancel);
                }
                */
            } while (pending.Count > 0);

            _Logger.LogInformation($"done");
        }

        public async Task FixGame(string gameID, CancellationToken cancel) {

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                _Logger.LogError($"game doesn't exist? [gameID={gameID}]");
                return;
            }

            Result<byte[], string> demofile = await _DemofileStorage.GetDemofileByFilename(match.FileName, cancel);
            if (demofile.IsOk == false) {
                _Logger.LogError($"failed to load demofile from storage [gameID={gameID}] [FileName={match.FileName}] [error={demofile.Error}]");
                return;
            }

            Result<BarMatch, string> parsed = await _Parser.Parse(match.FileName, demofile.Value, new DemofileParserOptions() {
                IncludeCommands = false,
                IncludeMapDraws = false,
            }, cancel);

            if (parsed.IsOk == false) {
                _Logger.LogError($"failed to parse demofile [gameID={gameID}] [error={parsed.Error}]");
                Debug.Fail($"failed to parse demofile");
            }

            Task[] tasks = [
                Task.Run(async () => {
                    await _MatchPlayerRepository.DeleteByGameID(gameID);
                    foreach (BarMatchPlayer player in parsed.Value.Players) {
                        await _MatchPlayerRepository.Insert(player);
                    }
                }, cancel),

                Task.Run(async () => {
                    await _MatchTeamRepository.DeleteByGameID(gameID);
                    foreach (BarMatchTeam team in parsed.Value.Teams) {
                        await _MatchTeamRepository.Insert(team, cancel);
                    }
                }, cancel)
            ];

            Task.WaitAll(tasks, cancel);

            BarMatchProcessing proc = await _ProcessingRepository.GetByGameID(gameID, cancel)
                ?? throw new InvalidOperationException($"missing {nameof(BarMatchProcessing)} {gameID}");

            proc.Features.Add("teams");
            await _ProcessingRepository.Upsert(proc);
        }

    }
}
