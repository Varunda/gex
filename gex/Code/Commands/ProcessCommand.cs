using gex.Commands;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class ProcessCommand {

        private readonly ILogger<ProcessCommand> _Logger;
        private readonly BaseQueue<GameReplayDownloadQueueEntry> _Queue;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogQueue;
		private readonly BarReplayApi _ReplayApi;
		private readonly BarReplayDb _ReplayDb;
		private readonly BarMatchProcessingRepository _ProcessingRepository;

        public ProcessCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<ProcessCommand>>();
            _Queue = services.GetRequiredService<BaseQueue<GameReplayDownloadQueueEntry>>();
            _ParseQueue = services.GetRequiredService<BaseQueue<GameReplayParseQueueEntry>>();
            _HeadlessRunQueue = services.GetRequiredService<BaseQueue<HeadlessRunQueueEntry>>();
            _ActionLogQueue = services.GetRequiredService<BaseQueue<ActionLogParseQueueEntry>>();
			_ReplayApi = services.GetRequiredService<BarReplayApi>();
			_ReplayDb = services.GetRequiredService<BarReplayDb>();
			_ProcessingRepository = services.GetRequiredService<BarMatchProcessingRepository>();
        }

		public async Task Recover(string gameID) {
			_Logger.LogInformation($"recovering game ID so it can be processed [gameID={gameID}]");

			BarReplay? replay = await _ReplayDb.GetByID(gameID);
			if (replay == null) {
				_Logger.LogInformation($"replay does not exist in DB, fetching from BAR API [gameID={gameID}]");
				Result<BarReplay, string> replayApi = await _ReplayApi.GetReplay(gameID, CancellationToken.None);
				if (replayApi.IsOk == false) {
					_Logger.LogError($"failed to recover match from BAR API, cannot recover match [gameID={gameID}]");
					return;
				}

				replay = replayApi.Value;
				await _ReplayDb.Insert(replay, CancellationToken.None);
				_Logger.LogInformation($"created replay info from BAR API [gameID={gameID}]");
			}

			BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, CancellationToken.None);
			if (proc == null) {
				_Logger.LogInformation($"match lacks processsing info, recreating [gameID={gameID}]");

				proc = new BarMatchProcessing();
				proc.GameID = gameID;
				await _ProcessingRepository.Upsert(proc);

				_Logger.LogInformation($"created processing info [gameID={gameID}]");
			}

			_Logger.LogInformation($"recovered match, can now be processed further [gameID={gameID}]");
		}

		public void Download(string gameID) {
            _Logger.LogInformation($"forcing download of game [gameID={gameID}]");
            _Queue.Queue(new GameReplayDownloadQueueEntry() {
                GameID = gameID,
                Force = true,
                ForceForward = true
            });
		}

        public void ForceRun(string gameID) {
            _Logger.LogInformation($"forcing a reprocess of game [gameID={gameID}]");
            _Queue.Queue(new GameReplayDownloadQueueEntry() {
                GameID = gameID,
                Force = true,
                ForceForward = true
            });
        }

        public void Parse(string gameID) {
            _Logger.LogInformation($"forcing demofile parse of game [gameID={gameID}]");
            _ParseQueue.Queue(new GameReplayParseQueueEntry() {
                GameID = gameID,
                Force = true,
                ForceForward = false
            });
        }

        public void Headless(string gameID) {
            _Logger.LogInformation($"forcing game to be ran headlessly [gameID={gameID}]");
            _HeadlessRunQueue.Queue(new HeadlessRunQueueEntry() {
                GameID = gameID,
                Force = true,
                ForceForward = true
            });
        }

        /// <summary>
        ///     command to re-parse the action log of a game
        /// </summary>
        /// <param name="gameID"></param>
        public void ParseActions(string gameID) {
            _Logger.LogInformation($"forcing a re-process of action log [gameID={gameID}]");
            _ActionLogQueue.Queue(new ActionLogParseQueueEntry() {
                GameID = gameID,
                Force = true
            });
        }

    }
}
