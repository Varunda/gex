using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class HeadlessRunQueueProcessor : BaseQueueProcessor<HeadlessRunQueueEntry> {

        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarHeadlessInstance _HeadlessRunner;
        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogParseQueue;

        public HeadlessRunQueueProcessor(ILoggerFactory factory, BaseQueue<HeadlessRunQueueEntry> queue,
            ServiceHealthMonitor serviceHealthMonitor, BarHeadlessInstance headlessRunner,
            IOptions<FileStorageOptions> options, BarMatchProcessingDb processingDb,
            BaseQueue<ActionLogParseQueueEntry> actionLogParseQueue)

        : base("headless_run_queue_processor", factory, queue, serviceHealthMonitor) {

            _HeadlessRunner = headlessRunner;
            _Options = options;
            _ProcessingDb = processingDb;
            _ActionLogParseQueue = actionLogParseQueue;
        }

        protected override async Task<bool> _ProcessQueueEntry(HeadlessRunQueueEntry entry, CancellationToken cancel) {

            _Logger.LogInformation($"running game headless [gameID={entry.GameID}] [force={entry.Force}]");

            Result<GameOutput, string> output = await _HeadlessRunner.RunGame(entry.GameID, entry.Force, cancel);

            if (output.IsOk == false) {
                _Logger.LogError($"failed to process game in headless mode [gameID={entry.GameID}] [error={output.Error}]");
                return true;
            }

            BarMatchProcessing processing = await _ProcessingDb.GetByGameID(entry.GameID)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.ReplaySimulated = DateTime.UtcNow;
            await _ProcessingDb.Upsert(processing);

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
