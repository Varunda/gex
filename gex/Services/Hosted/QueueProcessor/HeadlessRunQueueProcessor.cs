using gex.Common.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class HeadlessRunQueueProcessor : BaseQueueProcessor<HeadlessRunQueueEntry> {

        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarHeadlessInstance _HeadlessRunner;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogParseQueue;

        public HeadlessRunQueueProcessor(ILoggerFactory factory, BaseQueue<HeadlessRunQueueEntry> queue,
            ServiceHealthMonitor serviceHealthMonitor, BarHeadlessInstance headlessRunner,
            IOptions<FileStorageOptions> options, BarMatchProcessingRepository processingRepository,
            BaseQueue<ActionLogParseQueueEntry> actionLogParseQueue)

        : base("headless_run_queue_processor", factory, queue, serviceHealthMonitor) {

            _ThreadCount = 1;

            _HeadlessRunner = headlessRunner;
            _Options = options;
            _ProcessingRepository = processingRepository;
            _ActionLogParseQueue = actionLogParseQueue;
        }

        protected override async Task<bool> _ProcessQueueEntry(HeadlessRunQueueEntry entry, CancellationToken cancel) {

            _Logger.LogInformation($"running game headless [gameID={entry.GameID}] [force={entry.Force}]");

            Stopwatch timer = Stopwatch.StartNew();
            Result<GameOutput, string> output = await _HeadlessRunner.RunGame(entry.GameID, entry.Force, cancel);

            if (output.IsOk == false) {
                _Logger.LogError($"failed to process game in headless mode [gameID={entry.GameID}] [error={output.Error}]");
                return true;
            }

            BarMatchProcessing processing = await _ProcessingRepository.GetByGameID(entry.GameID, cancel)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.ReplaySimulated = DateTime.UtcNow;
            processing.ReplaySimulatedMs = (int)timer.ElapsedMilliseconds;
            await _ProcessingRepository.Upsert(processing);

            if (entry.ForceForward == true || processing.ActionsParsed == null) {
                _Logger.LogDebug($"putting entry into action log parser [gameID={entry.GameID}]");
                _ActionLogParseQueue.Queue(new ActionLogParseQueueEntry() {
                    GameID = entry.GameID,
                    Force = entry.Force
                });
            }

            return true;
        }

    }
}
