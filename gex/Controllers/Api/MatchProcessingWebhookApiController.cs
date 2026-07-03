using gex.Code;
using gex.Models;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Options;
using gex.Services;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/match-processing-webhook")]
    public class MatchProcessingWebhookApiController : ApiControllerBase {

        private readonly ILogger<MatchProcessingWebhookApiController> _Logger;
        private readonly MatchProcessingWebhookRepository _WebhookRepository;
        private readonly IOptions<InstanceOptions> _InstanceOptions;
        private readonly HttpUtilService _HttpUtil;
        private readonly IHttpContextAccessor _HttpContext;

        public MatchProcessingWebhookApiController(ILogger<MatchProcessingWebhookApiController> logger,
            HttpUtilService httpUtil, IHttpContextAccessor httpContext,
            MatchProcessingWebhookRepository webhookRepository, IOptions<InstanceOptions> instanceOptions) {

            _Logger = logger;
            _HttpUtil = httpUtil;
            _HttpContext = httpContext;
            _WebhookRepository = webhookRepository;
            _InstanceOptions = instanceOptions;
        }

        /// <summary>
        ///     create or refresh a webhook. the IP is stored when this is made.
        ///     when refreshing a webhook, the <paramref name="sharedSecret"/> is validated against the stored
        ///     shared secret of the existing <see cref="MatchProcessingWebhook"/>. if different, no refresh will occur
        /// </summary>
        /// <remarks>
        ///     <paramref name="type"/> can be 2 options:
        ///     <ul>
        ///         <li>parsed - sent when a <see cref="BarMatch"/> has been parsed</li>
        ///         <li>
        ///             replayed - sent when a <see cref="BarMatch"/> was replayed.
        ///             events will be included if <paramref name="includeEvents"/> is true (the default)
        ///         </li>
        ///     </ul>
        /// </remarks>
        /// <param name="url">URL to send matches to</param>
        /// <param name="type">type of matches wanted. parsed|replayed</param>
        /// <param name="sharedSecret">shared secret send to consumers to verify that gex is the sender. at most 256 characters</param>
        /// <param name="includeEvents">if the type is 'replayed', will the events be sent with the data?</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the request was completed. this does not indicate success or not, as if a webhook with the target url and type
        ///     already exists, and the shared secret does not match, no refresh takes place
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li><paramref name="type"/> was not parsed or replayed</li>
        ///         <li><paramref name="sharedSecret"/> was 0 characters long</li>
        ///         <li><paramref name="sharedSecret"/> was more than 256 characters</li>
        ///         <li><paramref name="url"/> was more than 1024 characters</li>
        ///         <li><paramref name="url"/> was a loopback URL</li>
        ///     </ul>
        /// </response>
        [HttpPost]
        [Authorize]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        public async Task<ApiResponse> CreateOrRefresh(
            [FromQuery] string url,
            [FromQuery] string type,
            [FromQuery] string sharedSecret,
            [FromQuery] bool includeEvents = true,
            CancellationToken cancel = default
        ) {

            if (_InstanceOptions.Value.EnableWebhooks == false) {
                return ApiForbidden($"webhooks are disabled by operator (hint: update the 'Instance' options in env.json)");
            }

            type = type.ToLower();
            if (type != "parsed" && type != "replayed") {
                return ApiBadRequest($"{nameof(type)} must be 'parsed'|'replayed'");
            }

            if (sharedSecret.Length == 0) {
                return ApiBadRequest($"{nameof(sharedSecret)} has to be at least 1 character");
            }
            if (sharedSecret.Length > 256) {
                return ApiBadRequest($"{nameof(sharedSecret)} cannot be more than 256 characters");
            }

            if (url.Length > 1024) {
                return ApiBadRequest($"{nameof(url)} cannot be than 1024 characters");
            }

            if (Uri.TryCreate(url, new UriCreationOptions(), out Uri? result) == false || result == null) {
                string? hint = null;

                if (url.StartsWith("http") == false) {
                    hint = "include http(s)";
                }

                return ApiBadRequest($"{nameof(url)} '{url}' must be a valid URI{(hint != null ? $". hint: {hint}" : "")}");
            }

            if (_InstanceOptions.Value.EnableWebhookLoopbackUrl == false && result.IsLoopback == true) {
                return ApiBadRequest($"loopback URLs not allowed (hint: update the 'Instance' options in env.json to allow this");
            }

            // if there is an existing webhook, and the shared secret is different, do not refresh the webhook
            MatchProcessingWebhook? existingHook = await _WebhookRepository.Get(url, type, cancel);
            if (existingHook != null && existingHook.SharedSecret != sharedSecret) {
                return ApiOk();
            }

            MatchProcessingWebhook webhook = new();
            webhook.Url = url;
            webhook.Type = type;
            webhook.IncludeEvents = includeEvents;
            webhook.SharedSecret = sharedSecret;
            webhook.Timestamp = DateTime.UtcNow;
            webhook.IP = _HttpUtil.GetHttpRemoteIp(_HttpContext.HttpContext) ?? "missing";

            await _WebhookRepository.Upsert(webhook, cancel);
            _Logger.LogDebug($"webhook created/refreshed [url={url}] [type={type}]");

            return ApiOk();
        }

        /// <summary>
        ///     delete a <see cref="MatchProcessingWebhook"/>. no success indication is given
        /// </summary>
        /// <param name="url">url of the webhook to delete</param>
        /// <param name="type">type of the webhook to delete</param>
        /// <param name="sharedSecret">shared secret of the webhook. must be correct, or the deletion will not work</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the operation was performed, but no indication if the webhook was successfully deleted or not is provided
        /// </response>
        /// <respone code="403">
        ///     webhooks are disabled
        /// </respone>
        [HttpDelete]
        public async Task<ApiResponse> Delete(
            [FromQuery] string url,
            [FromQuery] string type,
            [FromQuery] string sharedSecret,
            CancellationToken cancel = default
        ) {
            if (_InstanceOptions.Value.EnableWebhooks == false) {
                return ApiForbidden($"webhooks are disabled by operator (hint: update the 'Instance' options in env.json)");
            }

            MatchProcessingWebhook webhook = new();
            webhook.Url = url;
            webhook.Type = type.ToLower();
            webhook.SharedSecret = sharedSecret;

            await _WebhookRepository.Delete(webhook, cancel);

            return ApiOk();
        }

    }
}
