using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarReplayApi {

        private readonly ILogger<BarReplayApi> _Logger;
        private readonly BarApiMetric _Metric;

        private static readonly HttpClient _Http = new HttpClient();

        private const string BAR_API_URL = "https://api.bar-rts.com";

        private static readonly JsonSerializerOptions _JsonOptions;

        static BarReplayApi() {
            _JsonOptions = new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

        public BarReplayApi(ILogger<BarReplayApi> logger,
            BarApiMetric metric) {

            _Logger = logger;
            _Metric = metric;
        }

        /// <summary>
        ///     get recent <see cref="BarRecentReplay"/>s
        /// </summary>
        /// <param name="page">page to get</param>
        /// <param name="limit">limit</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<List<BarRecentReplay>, string>> GetRecent(int page = 1, int limit = 50, CancellationToken cancel = default) {
            Stopwatch timer = Stopwatch.StartNew();
            HttpResponseMessage response;
            try {
                response = await _Http.GetAsync(BAR_API_URL + $"/replays?page={page}&limit={limit}&hasBots=false&endedNormally=true", cancel);
            } catch (TaskCanceledException ex) {
                _Metric.RecordTimeout("recent");
                _Logger.LogError(ex, $"got timeout when loading recent games from BAR API");
                return $"got timeout when loading recent games from BAR API";
            } finally {
                _Metric.RecordDuration("recent", timer.ElapsedMilliseconds / 1000d);
                _Metric.RecordUse("recent");
            }

            if (response.IsSuccessStatusCode == false) {
                return $"failed to call bar API [status code={response.StatusCode}]";
            }

            byte[] body = await response.Content.ReadAsByteArrayAsync(cancel);

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body, _JsonOptions);
            if (json.ValueKind != JsonValueKind.Object) {
                return $"expected object from serialized JSON, got {json.ValueKind} instead";
            }

            JsonElement? jsonBody = json.GetChild("data");
            if (jsonBody == null) {
                return $"missing json element body from response";
            }

            List<BarRecentReplay> replays = [];

            foreach (JsonElement iter in jsonBody.Value.EnumerateArray()) {
                BarRecentReplay replay = new();
                replay.ID = iter.GetRequiredString("id");
                replay.StartTime = DateTime.Parse(iter.GetRequiredString("startTime"));

                replays.Add(replay);
            }

            return replays;
        }

        public async Task<Result<BarReplay, string>> GetReplay(string gameID, CancellationToken cancel = default) {
            string url = BAR_API_URL + "/replays/" + gameID;
            _Logger.LogDebug($"getting replay [gameID={gameID}] [url={url}]");

            Stopwatch timer = Stopwatch.StartNew();
            HttpResponseMessage response = await _Http.GetAsync(url, cancel);
            _Metric.RecordDuration("replay", timer.ElapsedMilliseconds / 1000d);
            _Metric.RecordUse("replay");

            if (response.IsSuccessStatusCode == false) {
                return $"failed to call bar API [status code={response.StatusCode}]";
            }

            byte[] body = await response.Content.ReadAsByteArrayAsync(cancel);
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body, _JsonOptions);

            if (json.ValueKind != JsonValueKind.Object) {
                return $"expected object from serialized JSON, got {json.ValueKind} instead";
            }

            BarReplay replay = new();
            replay.ID = json.GetRequiredString("id");
            replay.FileName = json.GetRequiredString("fileName");

            JsonElement? childNull = json.GetChild("Map");
            if (childNull != null) {
                replay.MapName = childNull.Value.GetString("fileName", "");
                //replay.MapName = childNull.Value.GetRequiredString("fileName");
            } else {
                _Logger.LogWarning($"missing Map from replay! [gameID={gameID}]");
            }

            return replay;
        }

    }
}
