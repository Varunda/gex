using Docker.DotNet.Models;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Apm;
using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace gex.Services.BarApi {

    public class ApmWebsiteApi {

        private readonly ILogger<ApmWebsiteApi> _Logger;
        private readonly IOptions<ApmWebsiteOptions> _Options;

        private static readonly HttpClient _Http = new();

        static ApmWebsiteApi() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex/0.1 (discord: varunda)");
            _Http.Timeout = TimeSpan.FromSeconds(20);
        }

        private const int MAX_RETRY_COUNT = 5;

        public ApmWebsiteApi(ILogger<ApmWebsiteApi> logger,
            IOptions<ApmWebsiteOptions> options) {

            _Logger = logger;
            _Options = options;

            if (_Options.Value.Enabled == true && string.IsNullOrWhiteSpace(_Options.Value.ApiUrl)) {
                throw new ArgumentException($"missing ApmWebsite:ApiUrl");
            }
            if (_Options.Value.Enabled == true && string.IsNullOrWhiteSpace(_Options.Value.ApiKey)) {
                throw new ArgumentException($"missing ApmWebsite:ApiKey");
            }

            _Http.BaseAddress = new Uri(_Options.Value.ApiUrl);
            _Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _Options.Value.ApiKey);
            _Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        ///     get the first page of <see cref="ApmTournamentEntry"/>s
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        public Task<Result<List<ApmTournamentEntry>, string>> GetEntries(CancellationToken cancel) {
            return GetEntries(1, cancel);
        }

        /// <summary>
        ///     get a page of <see cref="ApmTournamentEntry"/>s
        /// </summary>
        /// <param name="page">page number. 1 indexed</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<List<ApmTournamentEntry>, string>> GetEntries(int page, CancellationToken cancel) {
            _Logger.LogDebug($"getting page of tournament entries [page={page}]");

            if (page <= 0) {
                return $"{nameof(page)} must be at least 1";
            }

            Result<JsonElement, string> ret = await GetData($"tournaments?per_page=100&page={page}", JsonValueKind.Array, cancel);
            if (ret.IsOk == false) {
                return ret.Error;
            }

            JsonElement data = ret.Value;

            List<ApmTournamentEntry> entries = [];
            try {
                foreach (JsonElement iter in data.EnumerateArray()) {
                    ApmTournamentEntry entry = ApmTournamentEntry.Parse(iter);
                    entries.Add(entry);
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse tournament entries");
                return $"failed to parse tournament entries: {ex.Message}";
            }

            return entries;
        }

        /// <summary>
        ///     get the <see cref="ApmTournamentInfo"/> for a specific tournament
        /// </summary>
        /// <param name="id">ID of the tournament to get the info of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<ApmTournamentInfo, string>> GetTournamentInfo(string id, CancellationToken cancel) {
            Result<JsonElement, string> ret = await GetData($"tournaments/{HttpUtility.UrlEncode(id)}", JsonValueKind.Object, cancel);
            if (ret.IsOk == false) {
                return ret.Error;
            }

            JsonElement data = ret.Value;
            try {
                ApmTournamentInfo info = ApmTournamentInfo.Parse(data);
                return info;
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse tournament info [id={id}]");
                return $"failed to parse tournament info: {ex.Message}";
            }
        }

        /// <summary>
        ///     get a <see cref="JsonElement"/> that contains the data of a request, while handling all the error states
        /// </summary>
        /// <param name="url"></param>
        /// <param name="expectedKind"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<Result<JsonElement, string>> GetData(string url, JsonValueKind expectedKind, CancellationToken cancel) {
            if (_Options.Value.Enabled == false) {
                return "not enabled";
            }

            Result<ApmWebsiteResponse, string>? lastResponse = null;
            for (int i = 0; i < MAX_RETRY_COUNT; ++i) {
                Result<ApmWebsiteResponse, string> response = await GetDataInternal(url, i, cancel);
                lastResponse = response;
                if (response.IsOk == false) {
                    return response.Error;
                }

                if (response.Value.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
                    TimeSpan waitDuration = (response.Value.RateLimitRetryAfter - DateTime.UtcNow) + TimeSpan.FromSeconds(1);
                    if (waitDuration > TimeSpan.FromSeconds(61)) {
                        throw new InvalidOperationException($"wait duration is {waitDuration}, over the max of 61 seconds");
                    }

                    _Logger.LogDebug($"tripped rate limit, waiting [url={url}] [wait duration={waitDuration}] [retry after={response.Value.RateLimitRetryAfter}]");
                    await Task.Delay(waitDuration, cancel);
                } else {
                    break;
                }
            }

            if (lastResponse == null) {
                throw new InvalidOperationException($"expected a valid response");
            }

            if (lastResponse.IsOk == false) {
                return lastResponse.Error;
            }

            JsonElement json = lastResponse.Value.Json;

            if (json.ValueKind != JsonValueKind.Object) {
                return $"expected Object response, got {json.ValueKind} instead";
            }

            if (lastResponse.Value.StatusCode != System.Net.HttpStatusCode.OK) {
                string err = json.GetString("message", "<no message given>");

                return $"got bad response {lastResponse.Value.StatusCode}: {err}";
            }

            JsonElement data = json.GetRequiredChild("data");
            if (data.ValueKind != expectedKind) {
                return $"expected data element to {expectedKind}, got {data.ValueKind} instead";
            }

            return data;
        }

        private async Task<Result<ApmWebsiteResponse, string>> GetDataInternal(string url, int attempt, CancellationToken cancel) {
            if (_Options.Value.Enabled == false) {
                return "not enabled";
            }

            _Logger.LogDebug($"sending API request to apm website [url={url}] [try={attempt}]");

            Stopwatch timer = Stopwatch.StartNew();
            using HttpResponseMessage response = await _Http.GetAsync(url, cancel);

            int rateLimitCount = 0;
            {
                if (response.Headers.TryGetValues("X-RateLimit-Limit", out IEnumerable<string>? values) == true && values != null && values.Any()) {
                    rateLimitCount = int.Parse(values.ElementAt(0));
                }
            }

            int rateLimitLeft = 0;
            {
                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string>? values) == true && values != null && values.Any()) {
                    rateLimitLeft = int.Parse(values.ElementAt(0));
                }
            }

            DateTime rateLimitReset = DateTime.UtcNow;
            {
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out IEnumerable<string>? values) == true && values != null && values.Any()) {
                    rateLimitReset = DateTime.Parse(values.ElementAt(0)).ToUniversalTime();
                }
            }

            ApmWebsiteResponse ret = new ApmWebsiteResponse() {
                StatusCode = response.StatusCode,
                RateLimitRetryAfter = rateLimitReset,
                RateLimitCount = rateLimitCount,
                RateLimitLeft = rateLimitLeft
            };

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
                _Logger.LogWarning($"got rate limited from apm website [url={url}]");
                return ret;
            }

            _Logger.LogTrace($"got response from apm website [url={url}] [status={response.StatusCode}] [timer={timer.ElapsedMilliseconds}ms] "
                + $"[rate limit={rateLimitLeft}/{rateLimitCount} {rateLimitReset:u}]");

            using MemoryStream ms = new();
            await response.Content.CopyToAsync(ms, cancel);

            string body = Encoding.UTF8.GetString(ms.ToArray());
            JsonElement json;
            try {
                json = JsonSerializer.Deserialize<JsonElement>(body);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to deserialize json [url={url}]");
                return $"failed to deserialize json: {ex.Message}";
            }

            ret.Json = json;

            return ret;
        }

    }
}
