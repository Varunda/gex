using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class GameReplayDownloadQueueProcessor : BaseQueueProcessor<GameReplayDownloadQueueEntry> {

        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly BarReplayFileApi _ReplayFileApi;
        private readonly BarReplayDb _ReplayDb;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly IOptions<FileStorageOptions> _Options;

        public GameReplayDownloadQueueProcessor(ILoggerFactory factory,
            BaseQueue<GameReplayDownloadQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchProcessingDb processingDb, BarReplayFileApi replayFileApi,
            IOptions<FileStorageOptions> options, BarReplayDb replayDb,
            BaseQueue<GameReplayParseQueueEntry> parseQueue)

        : base("game_replay_download_queue", factory, queue, serviceHealthMonitor) {

            _ProcessingDb = processingDb;
            _ReplayFileApi = replayFileApi;
            _Options = options;
            _ReplayDb = replayDb;
            _ParseQueue = parseQueue;
        }

        protected override async Task<bool> _ProcessQueueEntry(GameReplayDownloadQueueEntry entry, CancellationToken cancel) {
            _Logger.LogInformation($"performing game replay download [gameID={entry.GameID}]");

            BarReplay? replay = await _ReplayDb.GetByID(entry.GameID);
            if (replay == null) {
                _Logger.LogError($"cannot download game replay: missing {nameof(BarReplay)} {entry.GameID}");
                return false;
            }

            string outputPath = Path.Join(_Options.Value.ReplayLocation, replay.FileName);

            if (File.Exists(outputPath) && entry.Force == false) {
                _Logger.LogInformation($"demofile already downloaded [gameID={entry.GameID}] [path={outputPath}]");
            } else {
                if (File.Exists(outputPath)) {
                    File.Delete(outputPath);
                }

                Result<byte[], string> result = await _ReplayFileApi.DownloadReplay(replay.FileName, cancel);
                if (result.IsOk == false) {
                    _Logger.LogError($"failed to download replay file {replay.FileName}: {result.Error}");
                } else {
                    using FileStream file = File.OpenWrite(outputPath);
                    await file.WriteAsync(result.Value, cancel);
                    _Logger.LogInformation($"successfully downloaded demofile [gameID={entry.GameID}] [outputPath={outputPath}]");
                }
            }

            BarMatchProcessing processing = await _ProcessingDb.GetByGameID(entry.GameID)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.ReplayDownloaded = DateTime.UtcNow;
            await _ProcessingDb.Upsert(processing);

            if (entry.ForceForward == true || processing.ReplayParsed == null) {
                _Logger.LogDebug($"putting entry into parse queue [gameID={entry.GameID}]");
                _ParseQueue.Queue(new GameReplayParseQueueEntry() {
                    GameID = entry.GameID,
                    FileName = replay.FileName,
                    Force = entry.Force,
                    ForceForward = entry.ForceForward
                });
            }

            return true;
        }

    }
}
