using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class GameReplayDownloadQueueProcessor : BaseQueueProcessor<GameReplayDownloadQueueEntry> {

        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BarReplayFileApi _ReplayFileApi;
        private readonly BarReplayDb _ReplayDb;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BarReplayApi _ReplayApi;
        private readonly BaseQueue<GameReplayDownloadQueueEntry> _DownloadQueue;
        private readonly BarDemofileParser _Parser;

        private readonly IOptions<FileStorageOptions> _Options;
        private readonly IOptions<FocusPlayerModeOptions> _FocusUserOptions;

        public GameReplayDownloadQueueProcessor(ILoggerFactory factory,
            BaseQueue<GameReplayDownloadQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchProcessingRepository processingRepository, BarReplayFileApi replayFileApi,
            IOptions<FileStorageOptions> options, BarReplayDb replayDb,
            BaseQueue<GameReplayParseQueueEntry> parseQueue, BarReplayApi replayApi,
            BaseQueue<GameReplayDownloadQueueEntry> downloadQueue, IOptions<FocusPlayerModeOptions> focusUserOptions,
            BarDemofileParser parser)
        : base("game_replay_download_queue", factory, queue, serviceHealthMonitor) {

            _ProcessingRepository = processingRepository;
            _ReplayFileApi = replayFileApi;
            _Options = options;
            _ReplayDb = replayDb;
            _ParseQueue = parseQueue;
            _ReplayApi = replayApi;
            _DownloadQueue = downloadQueue;
            _FocusUserOptions = focusUserOptions;
            _Parser = parser;
        }

        protected override async Task<bool> _ProcessQueueEntry(GameReplayDownloadQueueEntry entry, CancellationToken cancel) {
            _Logger.LogInformation($"performing game replay download [gameID={entry.GameID}]");
            Stopwatch timer = Stopwatch.StartNew();

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
                    _Logger.LogError($"failed to download replay file [FileName={replay.FileName}] [gameID={entry.GameID}] [error={result.Error}]");

                    if (result.Error == BarReplayFileApi.ERROR_DOWNLOAD_TIMEOUT) {
                        _Logger.LogWarning($"failed to download game, retrying [gameID={entry.GameID}]");
                        _DownloadQueue.Queue(entry);
                    }
                } else {

                    if (_FocusUserOptions.Value.Enabled == true) {
                        Result<BarMatch, string> parsed = await _Parser.Parse(replay.FileName, result.Value, new DemofileParserOptions(), cancel);

                        if (parsed.IsOk == false) {
                            _Logger.LogError($"failed to parse downloaded game [ID={replay.ID}] [filename={replay.FileName}] [error={parsed.Error}]");
                            return false;
                        }

                        BarMatchPlayer? found = parsed.Value.Players.FirstOrDefault(iter => _FocusUserOptions.Value.UserIDs.Contains(iter.UserID));
                        if (found == null) {
                            _Logger.LogDebug($"match has no players that are focused, skipping [id={replay.ID}]");
                            return true;
                        }
                    }

                    using FileStream file = File.OpenWrite(outputPath);
                    await file.WriteAsync(result.Value, cancel);
                    _Logger.LogInformation($"successfully downloaded demofile [gameID={entry.GameID}] [outputPath={outputPath}]");
                }
            }

            BarMatchProcessing processing = await _ProcessingRepository.GetByGameID(entry.GameID, cancel)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.ReplayDownloaded = DateTime.UtcNow;
            processing.ReplayDownloadedMs = (int)timer.ElapsedMilliseconds;
            await _ProcessingRepository.Upsert(processing);

            if (entry.ForceForward == true || processing.ReplayParsed == null) {
                _Logger.LogDebug($"putting entry into parse queue [gameID={entry.GameID}]");
                _ParseQueue.Queue(new GameReplayParseQueueEntry() {
                    GameID = entry.GameID,
                    Force = entry.Force,
                    ForceForward = entry.ForceForward
                });
            }

            return true;
        }

    }
}
