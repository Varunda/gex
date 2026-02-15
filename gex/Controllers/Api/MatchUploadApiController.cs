using gex.Code;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Queues;
using gex.Services;
using gex.Services.Db;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match-upload")]
    public class MatchUploadApiController : ApiControllerBase {

        private readonly ILogger<MatchUploadApiController> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarDemofileParser _DemofileParser;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarReplayDb _ReplayDb;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BarDemofileResultProcessor _DemofileProcessor;
        private readonly BarMapRepository _MapRepository;
        private readonly BarMatchPriorityCalculator _PriorityCalculator;
        private readonly BaseQueue<HeadlessRunQueueEntry> _RunQueue;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogParseQueue;
        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;
        private readonly ICurrentAccount _CurrentAccount;

        public MatchUploadApiController(ILogger<MatchUploadApiController> logger,
            BarMatchRepository matchRepository, BarDemofileParser demofileParser,
            BarMatchProcessingRepository processingRepository, IOptions<FileStorageOptions> options,
            BaseQueue<GameReplayParseQueueEntry> parseQueue, BarReplayDb replayDb,
            BarDemofileResultProcessor demofileProcessor, BarMapRepository mapRepository,
            BarMatchPriorityCalculator priorityCalculator, BaseQueue<HeadlessRunQueueEntry> runQueue,
            ICurrentAccount currentAccount, BaseQueue<ActionLogParseQueueEntry> actionLogParseQueue,
            MatchPoolRepository matchPoolRepository, MatchPoolEntryDb matchPoolEntryDb) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _DemofileParser = demofileParser;
            _ProcessingRepository = processingRepository;
            _Options = options;
            _ParseQueue = parseQueue;
            _ReplayDb = replayDb;
            _DemofileProcessor = demofileProcessor;
            _MapRepository = mapRepository;
            _PriorityCalculator = priorityCalculator;
            _RunQueue = runQueue;
            _CurrentAccount = currentAccount;
            _ActionLogParseQueue = actionLogParseQueue;
            _MatchPoolRepository = matchPoolRepository;
            _MatchPoolEntryDb = matchPoolEntryDb;
        }

        /// <summary>
        ///     upload a demofile, inserting it into the DB and processing it as if found from the BAR API
        /// </summary>
        /// <param name="cancel"></param>
        /// <response code="200">
        ///     the response will contain a JSON of the match that was uploaded
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li>the demofile failed to parse correctly, with the error being included</li>
        ///         <li>a match with the same ID as the demofile being uploaded already exists</li>
        ///         <li>the match is not saved in the DB, but has started processing</li>
        ///         <li>the match contains AI players, which Gex does not support</li>
        ///     </ul>
        /// </response>
        [HttpPost("upload")]
        [RequestTimeout(1000 * 60)] // allow 60 secs to upload
        [DisableFormValueModelBinding]
        [Authorize]
        [PermissionNeeded(AppPermission.GEX_MATCH_UPLOAD)]
        public async Task<ApiResponse<BarMatch>> Upload(CancellationToken cancel = default) {
            AppAccount? currentUser = await _CurrentAccount.Get(cancel);
            if (currentUser == null) {
                return ApiInternalError<BarMatch>($"bad state: current user is null?");
            }

            Stopwatch stepTimer = Stopwatch.StartNew();

            BarMatchProcessing processing = new();
            processing.ReplayDownloaded = DateTime.UtcNow;

            Result<byte[], ApiResponse<BarMatch>> data = await ReadMatchFromBody(cancel);
            if (data.IsOk == false) {
                return data.Error;
            }

            long uploadMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayDownloadedMs = (int)uploadMs;

            Result<BarMatch, string> parsed = await ParseGame(data.Value, cancel);
            if (parsed.IsOk == false) {
                return ApiBadRequest<BarMatch>($"failed to parse replay file: {parsed.Error}");
            }
            long parseMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayParsedMs = (int)parseMs;
            processing.ReplayParsed = DateTime.UtcNow;

            BarMatch match = parsed.Value;
            match.UploadedByID = currentUser.ID;

            BarMatch? existing = await _MatchRepository.GetByID(match.ID, cancel);
            if (existing != null) {
                _Logger.LogDebug($"demofile already exists in the database [gameID={match.ID}]");
                return ApiOk(existing);
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(match.ID, cancel);
            if (proc != null) {
                _Logger.LogDebug($"this game has been noticed and is processing in some way, but hasn't been parsed into a match yet");
                return ApiBadRequest<BarMatch>($"this game has already been seen, but is being processed, please wait!");
            }

            if (match.AiPlayers.Count > 0) {
                return ApiBadRequest<BarMatch>($"Games with AI players are not allowed to be uploaded");
            }

            _Logger.LogInformation($"new demofile has been uploaded [gameID={match.ID}]");

            string replayLocation = Path.Join(_Options.Value.ReplayLocation, match.FileName);
            if (System.IO.File.Exists(replayLocation) == true) {
                _Logger.LogError($"the replay file exists, but the DB data is missing! [gameID={match.ID}] [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: the database has inconsistent data, this is an unexpected state");
            }

            // at this point, we do NOT want to respect a user cancellation, as this could lead to inconsistent/half-written files
            using MemoryStream ms = new(data.Value);
            using FileStream fileStream = System.IO.File.OpenWrite(replayLocation);
            ms.Position = 0; // move back to start for copy to file
            await ms.CopyToAsync(fileStream, CancellationToken.None);

            if (System.IO.File.Exists(replayLocation) == false) {
                _Logger.LogError($"failsafe: the replay file must exist at the expected location at this point [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: missing replay file after copying it to the disk?");
            }

            processing.GameID = match.ID;
            processing.Priority = await _PriorityCalculator.Calculate(match, CancellationToken.None);
            await _ProcessingRepository.Upsert(processing);

            // while this isn't strictly necessary due to the info already being here, this is needed if re-parsing locally
            BarReplay replay = new();
            replay.ID = match.ID;
            replay.MapName = match.MapName;
            replay.FileName = match.FileName;
            await _ReplayDb.Insert(replay, CancellationToken.None);

            await _DemofileProcessor.Process(match, CancellationToken.None);

            if (processing.Priority == -1) {
                _RunQueue.Queue(new HeadlessRunQueueEntry() {
                    GameID = match.ID
                });
            }

            _Logger.LogInformation($"user uploaded match [gameID={match.ID}]");

            return ApiOk(match);
        }

        /// <summary>
        ///     upload method for remote processing. upload contains 4 files,
        ///     the demofile, actions.json, stdout.txt and stderr.txt. only usable via JWT auth and if the user claims
        ///     contains a familiar name claim
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpPost("upload-familiar")]
        [RequestTimeout(1000 * 60)] // allow 60 secs to upload
        [DisableFormValueModelBinding]
        [Authorize]
        public async Task<ApiResponse<BarMatch>> UploadFamiliar(CancellationToken cancel) {
            Claim? familiarClaim = Request.HttpContext.User.Claims.FirstOrDefault(iter => iter.Type == "familiar");
            if (familiarClaim == null) {
                return ApiForbidden<BarMatch>($"");
            }

            Stopwatch stepTimer = Stopwatch.StartNew();

            BarMatchProcessing processing = new();
            processing.ReplayDownloaded = DateTime.UtcNow;

            string contentType = Request.ContentType ?? "";
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase)) {
                return ApiBadRequest<BarMatch>($"ContentType '{contentType}' is not a multipart-upload");
            }

            MediaTypeHeaderValue type = MediaTypeHeaderValue.Parse(Request.ContentType);
            string boundary = HeaderUtilities.RemoveQuotes(type.Boundary).Value ?? "";

            if (string.IsNullOrWhiteSpace(boundary)) {
                return ApiBadRequest<BarMatch>($"boundary from ContentType '{Request.ContentType}' was is null or empty ({type}) ({type.Boundary})");
            }

            MultipartReader reader = new(boundary, Request.Body);
            MultipartSection? part = await reader.ReadNextSectionAsync(cancel);
            if (part == null) {
                return ApiBadRequest<BarMatch>($"Multipart section missing");
            }

            byte[] demofileBytes = [];
            byte[] actionsBytes = [];
            byte[] stdoutBytes = [];
            byte[] stderrBytes = [];

            do {
                bool hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(part.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition);
                if (hasContentDispositionHeader == false) {
                    return ApiInternalError<BarMatch>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
                }

                if (contentDisposition == null) {
                    return ApiInternalError<BarMatch>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
                }

                if (!HasFileContentDisposition(contentDisposition)) {
                    _Logger.LogError($"not a file content disposition [contentDisposition={contentDisposition}]");
                    return ApiBadRequest<BarMatch>($"not a file content disposition");
                }

                string? originalName = HeaderUtilities.RemoveQuotes(contentDisposition.FileName.Value ?? "").Value;
                using MemoryStream ms = new();
                await part.Body.CopyToAsync(ms, cancel);

                if (originalName == "demofile.sdfz") {
                    if (demofileBytes.Length > 0) {
                        return ApiBadRequest<BarMatch>($"demofile.sdfz already given");
                    }
                    demofileBytes = ms.ToArray();
                } else if (originalName == "actions.json") {
                    if (actionsBytes.Length > 0) {
                        return ApiBadRequest<BarMatch>($"actions.json already given");
                    }
                    actionsBytes = ms.ToArray();
                } else if (originalName == "stdout.txt") {
                    if (stdoutBytes.Length > 0) {
                        return ApiBadRequest<BarMatch>($"stdout.txt already given");
                    }
                    stdoutBytes = ms.ToArray();
                } else if (originalName == "stderr.txt") {
                    if (stderrBytes.Length > 0) {
                        return ApiBadRequest<BarMatch>($"stderr.txt already given");
                    }
                    stderrBytes = ms.ToArray();
                } else {
                    return ApiBadRequest<BarMatch>($"unexpected filename '{originalName}'");
                }

                part = await reader.ReadNextSectionAsync(cancel);
            } while (part != null);

            if (demofileBytes.Length == 0 || actionsBytes.Length == 0 || stdoutBytes.Length == 0 || stderrBytes.Length == 0) {
                return ApiBadRequest<BarMatch>($"missing file from upload");
            }

            long uploadMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayDownloadedMs = (int)uploadMs;

            Result<BarMatch, string> parsed = await _DemofileParser.Parse("demofile.sdfz", demofileBytes, new DemofileParserOptions(), cancel);
            if (parsed.IsOk == false) {
                return ApiBadRequest<BarMatch>($"failed to parse replay file: {parsed.Error}");
            }
            long parseMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayParsedMs = (int)parseMs;
            processing.ReplayParsed = DateTime.UtcNow;

            BarMatch match = parsed.Value;

            BarMatch? existing = await _MatchRepository.GetByID(match.ID, cancel);
            if (existing != null) {
                _Logger.LogDebug($"demofile already exists in the database [gameID={match.ID}]");
                return ApiOk(existing);
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(match.ID, cancel);
            if (proc != null) {
                _Logger.LogDebug($"this game has been noticed and is processing in some way, but hasn't been parsed into a match yet");
                return ApiBadRequest<BarMatch>($"this game has already been seen, but is being processed, please wait!");
            }

            if (match.AiPlayers.Count > 0) {
                return ApiBadRequest<BarMatch>($"Games with AI players are not allowed to be uploaded");
            }

            _Logger.LogInformation($"new demofile has been uploaded [gameID={match.ID}]");

            // 2025-02-02_20-35-40-247_Special Reef 1_2025.01.6.sdfz
            // {DATE}_{TIME_UTC}_{Map}_{Engine}.sdfz
            // it's impossible to recreate the correct demofile name from the info in the demofile
            //		(demofile does not have the millisecond start time, which the demofile uses)

            // https://github.com/beyond-all-reason/RecoilEngine/blob/0aa5469497c42b9066a304f13f76ea460aa69b07/rts/System/LoadSave/DemoRecorder.cpp#L156
            // map name gets truncated by one '.' for some reason
            //		ex: Map name_1.1 => Map_name_1
            string parsedName = $"{match.StartTime:yyyy-MM-dd}_{match.StartTime:HH-mm-ss-fff}_{match.Map}_{match.Engine}.sdfz";
            match.FileName = parsedName;

            string replayLocation = Path.Join(_Options.Value.ReplayLocation, parsedName);
            if (System.IO.File.Exists(replayLocation) == true) {
                _Logger.LogError($"the replay file exists, but the DB data is missing! [gameID={match.ID}] [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: the database has inconsistent data, this is an unexpected state");
            }

            match.MapName = (await _MapRepository.GetByName(match.Map, CancellationToken.None))?.FileName ?? "";

            // at this point, we do NOT want to respect a user cancellation, as this could lead to inconsistent/half-written files
            using FileStream fileStream = System.IO.File.OpenWrite(replayLocation);
            await fileStream.WriteAsync(demofileBytes, CancellationToken.None);

            if (System.IO.File.Exists(replayLocation) == false) {
                _Logger.LogError($"failsafe: the replay file must exist at the expected location at this point [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: missing replay file after copying it to the disk?");
            }

            processing.GameID = match.ID;
            processing.Priority = await _PriorityCalculator.Calculate(match, CancellationToken.None);
            await _ProcessingRepository.Upsert(processing);

            // while this isn't strictly necessary due to the info already being here, this is needed if re-parsing locally
            BarReplay replay = new();
            replay.ID = match.ID;
            replay.MapName = match.MapName;
            replay.FileName = parsedName;
            await _ReplayDb.Insert(replay, CancellationToken.None);

            await _DemofileProcessor.Process(match, CancellationToken.None);

            string gamePrefixLocation = Path.Join(_Options.Value.GameLogLocation, match.ID.Substring(0, 2));
            Directory.CreateDirectory(gamePrefixLocation);

            string gameLogLocation = Path.Join(gamePrefixLocation, match.ID);
            string gameActionLogPath = gameLogLocation + Path.DirectorySeparatorChar + "actions.json";

            // copy logs to folder
            if (Directory.Exists(gameLogLocation) == false) {
                Directory.CreateDirectory(gameLogLocation);
            }

            string stdoutLogs = gameLogLocation + Path.DirectorySeparatorChar + "stdout.txt";
            string stderrLogs = gameLogLocation + Path.DirectorySeparatorChar + "stderr.txt";
            await System.IO.File.WriteAllBytesAsync(stdoutLogs, stdoutBytes, cancel);
            await System.IO.File.WriteAllBytesAsync(stderrLogs, stderrBytes, cancel);

            _Logger.LogDebug($"writing action log [loc={Path.Join(gameLogLocation, "actions.json")}");
            await System.IO.File.WriteAllBytesAsync(Path.Join(gameLogLocation, "actions.json"), actionsBytes);
            _ActionLogParseQueue.Queue(new ActionLogParseQueueEntry() {
                GameID = match.ID,
            });

            _Logger.LogInformation($"familiar uploaded match [gameID={match.ID}]");

            return ApiOk(match);
        }

        /// <summary>
        ///     special method for a third party site to use Gex (such as the APM website)
        /// </summary>
        /// <param name="matchPoolID">optional, the ID of the <see cref="MatchPool"/> to insert the match into</param>
        /// <param name="prioritize">if the match will be prioritized at prio 9 or if normal rules will be used</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     
        /// </response>
        [HttpPost("upload-third-party")]
        [RequestTimeout(1000 * 60)] // allow 60 secs to upload
        [DisableFormValueModelBinding]
        [Authorize]
        public async Task<ApiResponse<BarMatch>> UploadThirdParty(int? matchPoolID = null,
            bool? prioritize = false,
            CancellationToken cancel = default) {

            Claim? claim = Request.HttpContext.User.Claims.FirstOrDefault(iter => iter.Type == "thirdparty_website");
            if (claim == null) {
                return ApiForbidden<BarMatch>($"this endpoint is only usable with the correct user claim");
            }

            if (matchPoolID != null) {
                if (await _MatchPoolRepository.GetByID(matchPoolID.Value, cancel) == null) {
                    return ApiNotFound<BarMatch>($"{nameof(MatchPool)} {matchPoolID}");
                }
            }

            Stopwatch stepTimer = Stopwatch.StartNew();

            BarMatchProcessing processing = new();
            processing.ReplayDownloaded = DateTime.UtcNow;

            Result<byte[], ApiResponse<BarMatch>> data = await ReadMatchFromBody(cancel);
            if (data.IsOk == false) {
                return data.Error;
            }

            long uploadMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayDownloadedMs = (int)uploadMs;

            Result<BarMatch, string> parsed = await ParseGame(data.Value, cancel);
            if (parsed.IsOk == false) {
                return ApiBadRequest<BarMatch>($"failed to parse replay file: {parsed.Error}");
            }
            long parseMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayParsedMs = (int)parseMs;
            processing.ReplayParsed = DateTime.UtcNow;

            BarMatch match = parsed.Value;

            BarMatch? existing = await _MatchRepository.GetByID(match.ID, cancel);
            if (existing != null) {
                _Logger.LogDebug($"demofile already exists in the database [gameID={match.ID}]");
                return ApiOk(existing);
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(match.ID, cancel);
            if (proc != null) {
                _Logger.LogDebug($"this game has been noticed and is processing in some way, but hasn't been parsed into a match yet [gameID={match.ID}]");
                return ApiBadRequest<BarMatch>($"this game has already been seen, but is being processed, please wait!");
            }

            if (match.AiPlayers.Count > 0) {
                return ApiBadRequest<BarMatch>($"Games with AI players are not allowed to be uploaded");
            }

            _Logger.LogInformation($"new demofile has been uploaded [gameID={match.ID}]");

            string replayLocation = Path.Join(_Options.Value.ReplayLocation, match.FileName);
            if (System.IO.File.Exists(replayLocation) == true) {
                _Logger.LogError($"the replay file exists, but the DB data is missing! [gameID={match.ID}] [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: the database has inconsistent data, this is an unexpected state");
            }

            // at this point, we do NOT want to respect a user cancellation, as this could lead to inconsistent/half-written files
            using FileStream fileStream = System.IO.File.OpenWrite(replayLocation);
            await fileStream.WriteAsync(data.Value, CancellationToken.None);

            if (System.IO.File.Exists(replayLocation) == false) {
                _Logger.LogError($"failsafe: the replay file must exist at the expected location at this point [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: missing replay file after copying it to the disk?");
            }

            processing.GameID = match.ID;
            processing.Priority = (prioritize == true) ? ((short)9) : (await _PriorityCalculator.Calculate(match, CancellationToken.None));
            await _ProcessingRepository.Upsert(processing);

            // while this isn't strictly necessary due to the info already being here, this is needed if re-parsing locally
            BarReplay replay = new();
            replay.ID = match.ID;
            replay.MapName = match.MapName;
            replay.FileName = match.FileName;
            await _ReplayDb.Insert(replay, CancellationToken.None);

            await _DemofileProcessor.Process(match, CancellationToken.None);

            if (processing.Priority == -1) {
                _RunQueue.Queue(new HeadlessRunQueueEntry() {
                    GameID = match.ID
                });
            }

            if (matchPoolID != null) {
                await _MatchPoolEntryDb.Insert(new MatchPoolEntry() {
                    PoolID = matchPoolID.Value,
                    MatchID = match.ID,
                    AddedByID = AppAccount.Root,
                    Timestamp = DateTime.UtcNow
                }, CancellationToken.None);
            }

            _Logger.LogInformation($"third party website uploaded match [gameID={match.ID}] [method=third party website] [matchPoolID={matchPoolID}]");

            return ApiOk(match);
        }

        /// <summary>
        ///     return a parsed <see cref="BarMatch"/> from an uploaded demofile, but does not actually save
        ///     the demofile or process it further. this is useful for 3rd party sites to use.
        ///     sometimes this can return more info than an upload would, in the case where an upload already
        ///     exists
        /// </summary>
        /// <param name="cancel"></param>
        /// <response code="200">
        ///     the response will contain a JSON that represents what Gex would store about a demofile,
        ///     but does not actually store the demofile or process it further
        /// </response>
        [HttpPost("inspect")]
        [RequestTimeout(1000 * 60)] // allow 60 secs to upload
        [DisableFormValueModelBinding]
        [Authorize]
        public async Task<ApiResponse<BarMatch>> InspectDemofile(CancellationToken cancel = default) {
            Result<byte[], ApiResponse<BarMatch>> data = await ReadMatchFromBody(cancel);
            if (data.IsOk == false) {
                return data.Error;
            }

            Result<BarMatch, string> parsed = await ParseGame(data.Value, cancel);

            BarMatch match = parsed.Value;
            return ApiOk(match);
        }

        internal static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

        /// <summary>
        ///     read the uploaded demofile from the request, returning the <see cref="BarMatch"/> on success,
        ///     or a <see cref="ApiResponse{T}"/> that contains the error type and error message on failure
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        internal async Task<Result<byte[], ApiResponse<BarMatch>>> ReadMatchFromBody(CancellationToken cancel) {
            string contentType = Request.ContentType ?? "";
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase)) {
                return ApiBadRequest<BarMatch>($"ContentType '{contentType}' is not a multipart-upload");
            }

            MediaTypeHeaderValue type = MediaTypeHeaderValue.Parse(Request.ContentType);
            string boundary = HeaderUtilities.RemoveQuotes(type.Boundary).Value ?? "";

            if (string.IsNullOrWhiteSpace(boundary)) {
                return ApiBadRequest<BarMatch>($"boundary from ContentType '{Request.ContentType}' was is null or empty ({type}) ({type.Boundary})");
            }

            MultipartReader reader = new(boundary, Request.Body);
            MultipartSection? part = await reader.ReadNextSectionAsync(cancel);

            if (part == null) {
                return ApiBadRequest<BarMatch>($"Multipart section missing");
            }

            bool hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(part.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition);
            if (hasContentDispositionHeader == false) {
                return ApiInternalError<BarMatch>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
            }

            if (contentDisposition == null) {
                return ApiInternalError<BarMatch>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
            }

            if (!HasFileContentDisposition(contentDisposition)) {
                _Logger.LogError($"not a file content disposition [contentDisposition={contentDisposition}]");
                return ApiBadRequest<BarMatch>($"not a file content disposition");
            }

            string originalName = contentDisposition.FileName.Value ?? "";
            _Logger.LogDebug($"demofile uploaded [originalName={originalName}]");

            string? extension = HeaderUtilities.RemoveQuotes(Path.GetExtension(originalName)).Value;
            if (string.IsNullOrWhiteSpace(extension)) {
                return ApiBadRequest<BarMatch>($"extension from name {originalName} is null or empty");
            }

            if (extension != ".sdfz") {
                return ApiBadRequest<BarMatch>($"expected .sdfz as file extension, got '{extension}' instead");
            }

            using MemoryStream ms = new();
            await part.Body.CopyToAsync(ms, cancel);

            byte[] data = ms.ToArray();
            return data;
        }

        /// <summary>
        ///     parse the bytes of demofile, updating the <see cref="BarMatch.FileName"/> and <see cref="BarMatch.MapName"/>
        ///     to safe values
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<Result<BarMatch, string>> ParseGame(byte[] data, CancellationToken cancel) {
            Result<BarMatch, string> parsed = await _DemofileParser.Parse("demofile.sdfz", data, new DemofileParserOptions(), cancel);
            if (parsed.IsOk == false) {
                return parsed.Error;
            }

            BarMatch match = parsed.Value;
            string parsedName = $"{match.StartTime:yyyy-MM-dd}_{match.StartTime:HH-mm-ss-fff}_{match.Map}_{match.Engine}.sdfz";

            // do not accept the name of the file the user provides. this could be used to overwrite data
            // 2025-02-02_20-35-40-247_Special Reef 1_2025.01.6.sdfz
            // {DATE}_{TIME_UTC}_{Map}_{Engine}.sdfz
            // it's impossible to recreate the correct demofile name from the info in the demofile
            //		(demofile does not have the millisecond start time, which the demofile uses)
            match.FileName = parsedName;
            match.MapName = (await _MapRepository.GetByName(match.Map, CancellationToken.None))?.FileName ?? "";

            return match;
        }

    }
}
