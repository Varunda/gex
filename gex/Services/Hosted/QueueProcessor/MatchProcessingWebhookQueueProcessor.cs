using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Services.Repository.Match;
using gex.Models.Db;
using gex.Models.Event;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace gex.Services.Hosted.QueueProcessor {

    public class MatchProcessingWebhookQueueProcessor : BaseQueueProcessor<MatchProcessingWebhookQueueEntry> {

        private readonly MatchProcessingWebhookRepository _WebhookRepository;
        private readonly BarMatchRepository _MatchRepository;
        private readonly GameOutputRepository _OutputRepository;
        private readonly IOptions<MatchProcessingWebhookOptions> _Options;
        private readonly MatchProcessingWebhookUtil _WebhookUtil;

        private static HttpClient _Http = new HttpClient();

        static MatchProcessingWebhookQueueProcessor() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex-webhooks/0.1");
            _Http.Timeout = TimeSpan.FromSeconds(5);
        }

        public MatchProcessingWebhookQueueProcessor(ILoggerFactory factory,
            BaseQueue<MatchProcessingWebhookQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            MatchProcessingWebhookRepository webhookRepository, BarMatchRepository matchRepository,
            GameOutputRepository outputRepository, IOptions<MatchProcessingWebhookOptions> options,
            MatchProcessingWebhookUtil webhookUtil)
        : base("match_processing_webhook_queue", factory, queue, serviceHealthMonitor) {

            _WebhookRepository = webhookRepository;
            _MatchRepository = matchRepository;
            _OutputRepository = outputRepository;
            _Options = options;

            if (_Options.Value.Proxy != null) {
                try {
                    Uri uri = new(_Options.Value.Proxy);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to validate proxy URL as a valid URL [proxy={_Options.Value.Proxy}]");
                    throw;
                }

                if (string.IsNullOrWhiteSpace(_Options.Value.ProxySecret) == true) {
                    throw new ArgumentException($"MatchProcessingWebhook.ProxySecret cannot be empty if a proxy is given");
                }
            }

            _WebhookUtil = webhookUtil;
        }

        protected override async Task<bool> _ProcessQueueEntry(MatchProcessingWebhookQueueEntry entry, CancellationToken cancel) {
            _Logger.LogDebug($"processing webhook type [gameID={entry.GameID}] [type={entry.Type}]");

            List<MatchProcessingWebhook> webhooks = await _WebhookRepository.GetAll(cancel);
            if (webhooks.Count == 0) {
                return false;
            }

            Result<JsonObject, string> root = await _WebhookUtil.BuildBody(entry.GameID,
                includeOutput: entry.Type == MatchProcessingWebhookQueueEntry.REPLAYED, cancel);

            if (root.IsOk == false) {
                _Logger.LogError($"failed to build body for webhook [error={root.Error}] [gameID={entry.GameID}] [type={entry.Type}]");
                return false;
            }

            // for webhooks that want games that are replayed, but without events, give those a different JsonObject
            JsonObject rootNoEvents = JsonSerializer.Deserialize<JsonObject>("{}")!;
            if (root.Value.TryGetPropertyValue("match", out JsonNode? matchNode) == true && matchNode != null) {
                rootNoEvents.Add("match", matchNode.DeepClone());
            } else {
                _Logger.LogError($"failed to get 'match' property from root JSON to add to rootNoEvents");
            }

            foreach (MatchProcessingWebhook webhook in webhooks) {
                if (webhook.Type != entry.Type) {
                    continue;
                }

                JsonObject json = (entry.Type == MatchProcessingWebhookQueueEntry.REPLAYED && webhook.IncludeEvents == false)
                    ? rootNoEvents : root.Value;

                await _WebhookUtil.SendToWebhook(webhook, json, cancel);
            }

            return true;
        }

    }
}
