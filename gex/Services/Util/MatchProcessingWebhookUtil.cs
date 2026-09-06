using gex.Code.Converters;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace gex.Services.Util {

    public class MatchProcessingWebhookUtil {

        private readonly ILogger<MatchProcessingWebhookUtil> _Logger;
        private readonly MatchProcessingWebhookRepository _WebhookRepository;
        private readonly BarMatchRepository _MatchRepository;
        private readonly GameOutputRepository _OutputRepository;
        private readonly IBarMatchBuilderUtil _MatchBuilder;
        private readonly IOptions<MatchProcessingWebhookOptions> _Options;

        private static HttpClient _Http = new HttpClient();

        static MatchProcessingWebhookUtil() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex-webhooks/0.1");
            _Http.Timeout = TimeSpan.FromSeconds(5);
        }

        public MatchProcessingWebhookUtil(ILogger<MatchProcessingWebhookUtil> logger,
            MatchProcessingWebhookRepository webhookRepository, BarMatchRepository matchRepository,
            GameOutputRepository outputRepository, IOptions<MatchProcessingWebhookOptions> options,
            IBarMatchBuilderUtil matchBuilder) {

            _Logger = logger;
            _WebhookRepository = webhookRepository;
            _MatchRepository = matchRepository;
            _OutputRepository = outputRepository;
            _Options = options;
            _MatchBuilder = matchBuilder;
        }

        /// <summary>
        ///     build a JSON node that will be sent to webhooks
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        /// <param name="includeOutput">if the node will include the game output</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<JsonObject, string>> BuildBody(string gameID, bool includeOutput, CancellationToken cancel) {
            Result<Maybe<BarMatch>, string> built = await _MatchBuilder.BuildMatch(gameID, new IBarMatchBuilderUtil.BuildOptions() {
                IncludeTeams = true,
                IncludeAllyTeams = true,
                IncludePlayers = true,
                IncludeSpectators = true,
                IncludeStartRegionData = true,
            }, null, cancel);

            if (built.IsOk == false) {
                _Logger.LogError($"failed to build match [gameID={gameID}] [error={built.Error}]");
                return "failed to build match";
            }

            if (built.Value.Has() == false) {
                _Logger.LogWarning($"missing match to send webhook for [gameID={gameID}]");
                return "no match for gameID";
            }

            JsonSerializerOptions opts = new();
            opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.Converters.Add(new DateTimeJsonConverter());
            opts.Converters.Add(new TimeSpanJsonConverter());
            opts.Converters.Add(new Vector3JsonConverter());

            BarMatch match = built.Value.Get();

            JsonNode json = JsonSerializer.SerializeToNode(match, opts)!;

            JsonObject root = JsonSerializer.Deserialize<JsonObject>("{}")!;
            root.Add("match", json);

            if (includeOutput == true) {
                Result<GameOutput?, string> result = await _OutputRepository.Build(gameID, new GameOutputRepository.BuildOptions() {
                    IncludeCommanderPositionUpdates = true,
                    IncludeExtraStats = true,
                    IncludeFactoryUnitCreate = true,
                    IncludeTeamDiedEvents = true,
                    IncludeTeamStats = true,
                    IncludeTransportLoads = true,
                    IncludeTransportUnloads = true,
                    IncludeUnitDamage = true,
                    IncludeUnitDefs = true,
                    IncludeUnitPosition = true,
                    IncludeUnitResources = true,
                    IncludeUnitsCreated = true,
                    IncludeUnitsGiven = true,
                    IncludeUnitsKilled = true,
                    IncludeUnitsTaken = true,
                    IncludeWindUpdates = true,
                }, null, cancel);

                if (result.IsOk == false) {
                    _Logger.LogError($"failed to load game output [gameID={gameID}] [error={result.Error}]");
                    return "failed to make game output";
                }

                if (result.Value == null) {
                    _Logger.LogError($"missing game output [gameID={gameID}]");
                    return "missing game output";
                }

                root.Add("output", JsonSerializer.SerializeToNode(result.Value, opts));
            }

            return root;
        }

        /// <summary>
        ///     send a JSON object to a webhook
        /// </summary>
        /// <param name="webhook">webhook to send the JSON to</param>
        /// <param name="root">JSON to be sent thru the webhook</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task SendToWebhook(MatchProcessingWebhook webhook, JsonObject root, CancellationToken cancel) {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            try {
                HttpRequestMessage req = new(HttpMethod.Post, webhook.Url);
                req.Headers.Authorization = new AuthenticationHeaderValue("SharedSecret", webhook.SharedSecret);
                req.Content = JsonContent.Create(root);

                if (_Options.Value.Proxy != null) {
                    req.RequestUri = new Uri(_Options.Value.Proxy + $"?Target={HttpUtility.UrlEncode(webhook.Url)}");
                    req.Headers.TryAddWithoutValidation("ProxySecret", _Options.Value.ProxySecret);
                }

                HttpResponseMessage response = await _Http.SendAsync(req, cancel);
                if (_Options.Value.Proxy != null) {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                        string body = await response.Content.ReadAsStringAsync(cancel);
                        _Logger.LogWarning($"got non-200 response code [statusCode={response.StatusCode}] [body={body.Truncate(100)}]");
                    }
                }

                _Logger.LogDebug($"sent POST request [url={webhook.Url}{(_Options.Value.ProxySecret != null ? " (proxied)":"")}] [type={webhook.Type}]");
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to send webhook to url [exception={ex.Message}] [url={webhook.Url}]");
            }
        }


    }
}
