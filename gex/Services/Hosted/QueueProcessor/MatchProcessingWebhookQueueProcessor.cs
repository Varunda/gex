using gex.Common.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Queues;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class MatchProcessingWebhookQueueProcessor : BaseQueueProcessor<MatchProcessingWebhookQueueEntry> {

        private readonly MatchProcessingWebhookRepository _WebhookRepository;
        private readonly BarMatchRepository _MatchRepository;
        private readonly GameOutputRepository _OutputRepository;

        private static HttpClient _Http = new HttpClient();

        static MatchProcessingWebhookQueueProcessor() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex-webhooks/0.1");
        }

        public MatchProcessingWebhookQueueProcessor(ILoggerFactory factory,
            BaseQueue<MatchProcessingWebhookQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            MatchProcessingWebhookRepository webhookRepository, BarMatchRepository matchRepository,
            GameOutputRepository outputRepository)
        : base("match_processing_webhook_queue", factory, queue, serviceHealthMonitor) {

            _WebhookRepository = webhookRepository;
            _MatchRepository = matchRepository;
            _OutputRepository = outputRepository;
        }

        protected override async Task<bool> _ProcessQueueEntry(MatchProcessingWebhookQueueEntry entry, CancellationToken cancel) {

            _Logger.LogDebug($"processing webhook type [gameID={entry.GameID}] [type={entry.Type}]");

            List<MatchProcessingWebhook> webhooks = await _WebhookRepository.GetAll(cancel);
            if (webhooks.Count == 0) {
                return false;
            }

            Result<BarMatch?, string> built = await _MatchRepository.BuildMatch(entry.GameID, new BarMatchRepository.BuildOptions() {
                IncludeAllyTeams = true,
                IncludePlayers = true,
            }, null, cancel);
            if (built.IsOk == false) {
                _Logger.LogError($"failed to build match [gameID={entry.GameID}] [error={built.Error}]");
                return true;
            }

            if (built.Value == null) {
                _Logger.LogWarning($"missing match to send webhook for [gameID={entry.GameID}]");
                return false;
            }

            JsonSerializerOptions opts = new JsonSerializerOptions();
            opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            BarMatch match = built.Value;

            JsonNode json = JsonSerializer.SerializeToNode(match, opts)!;

            JsonObject root = JsonSerializer.Deserialize<JsonObject>("{}")!;
            root.Add("match", json);

            // for webhooks that want games that are replayed, but without events, give those a different JsonObject
            JsonObject rootNoEvents = JsonSerializer.Deserialize<JsonObject>("{}")!;
            if (entry.Type == MatchProcessingWebhookQueueEntry.REPLAYED) {
                rootNoEvents.Add("match", json.DeepClone());

                Result<GameOutput?, string> result = await _OutputRepository.Build(entry.GameID, new GameOutputRepository.BuildOptions() {
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
                    _Logger.LogError($"failed to load game output [gameID={entry.GameID}] [error={result.Error}]");
                    return true;
                }

                if (result.Value == null) {
                    _Logger.LogError($"missing game output [gameID={entry.GameID}]");
                    Debug.Fail("huh");
                    return true;
                }

                root.Add("output", JsonSerializer.SerializeToNode(result.Value, opts));
            }

            foreach (MatchProcessingWebhook webhook in webhooks) {
                if (webhook.Type != entry.Type) {
                    continue;
                }

                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

                try {
                    if (entry.Type == MatchProcessingWebhookQueueEntry.REPLAYED && webhook.IncludeEvents == false) {
                        await _Http.PostAsJsonAsync(webhook.Url, rootNoEvents, opts, cts.Token);
                        _Logger.LogDebug($"sent POST request [url={webhook.Url}] [type={entry.Type}]");
                    } else {
                        await _Http.PostAsJsonAsync(webhook.Url, root, opts, cts.Token);
                        _Logger.LogDebug($"sent POST request [url={webhook.Url}] [type={entry.Type}]");
                    }
                } catch (Exception ex) {
                    _Logger.LogWarning($"failed to send webhook to url [exception={ex.Message}] [url={webhook.Url}]");
                }
            }

            return true;
        }

    }
}
