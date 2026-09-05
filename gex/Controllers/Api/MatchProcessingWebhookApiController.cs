using gex.Code;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Services.Repository.Match;
using gex.Models;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Options;
using gex.Services;
using gex.Services.Db;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/match-processing-webhook")]
    public class MatchProcessingWebhookApiController : ApiControllerBase {

        private readonly ILogger<MatchProcessingWebhookApiController> _Logger;
        private readonly MatchProcessingWebhookRepository _WebhookRepository;
        private readonly MatchProcessingWebhookUtil _WebhookUtil;
        private readonly BarMatchRepository _MatchRepository;

        private readonly IOptions<InstanceOptions> _InstanceOptions;
        private readonly HttpUtilService _HttpUtil;
        private readonly IHttpContextAccessor _HttpContext;
        private readonly ICurrentAccount _CurrentAccount;

        public MatchProcessingWebhookApiController(ILogger<MatchProcessingWebhookApiController> logger,
            HttpUtilService httpUtil, IHttpContextAccessor httpContext,
            MatchProcessingWebhookRepository webhookRepository, IOptions<InstanceOptions> instanceOptions,
            ICurrentAccount currentAccount, MatchProcessingWebhookUtil webhookUtil,
            BarMatchRepository matchRepository) {

            _Logger = logger;
            _HttpUtil = httpUtil;
            _HttpContext = httpContext;
            _WebhookRepository = webhookRepository;
            _InstanceOptions = instanceOptions;
            _CurrentAccount = currentAccount;
            _WebhookUtil = webhookUtil;
            _MatchRepository = matchRepository;
        }

        /// <summary>
        ///     get the webhooks of the user making the request
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <respone code="200">
        ///     the response will contain a list of <see cref="MatchProcessingWebhook"/>s that the user
        ///     making the request has
        /// </respone>
        [HttpGet]
        [Authorize]
        public async Task<ApiResponse<List<MatchProcessingWebhook>>> GetByCurrentUser(CancellationToken cancel = default) {
            AppAccount? currentUser = await _CurrentAccount.Get(cancel);
            if (currentUser == null) {
                return ApiInternalError<List<MatchProcessingWebhook>>($"no current user");
            }

            List<MatchProcessingWebhook> userHooks = (await _WebhookRepository.GetAll(cancel))
                .Where(iter => iter.UserID == currentUser.ID)
                .ToList();

            return ApiOk(userHooks);
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
        public async Task<ApiResponse> CreateOrRefresh(
            [FromQuery] string url,
            [FromQuery] string type,
            [FromQuery] string sharedSecret,
            [FromQuery] bool includeEvents = true,
            CancellationToken cancel = default
        ) {

            type = type.ToLower();

            AppAccount? currentUser = await _CurrentAccount.Get(cancel);
            if (currentUser == null) {
                return ApiInternalError($"no current user");
            }

            if (_InstanceOptions.Value.EnableWebhooks == false) {
                return ApiForbidden($"webhooks are disabled by operator (hint: update the 'Instance' options in env.json)");
            }

            List<MatchProcessingWebhook> userHooks = (await _WebhookRepository.GetAll(cancel)).Where(iter => iter.UserID == currentUser.ID).ToList();
            if (userHooks.Count >= 10) {
                return ApiBadRequest($"a user can have at most 10 webhooks");
            }

            string? validationError = _Validate(url, type, sharedSecret);
            if (validationError != null) {
                return ApiBadRequest(validationError);
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
            webhook.UserID = currentUser.ID;

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

        /// <summary>
        ///     send a test payload to a webhook
        /// </summary>
        /// <param name="url">URL to send the payload to</param>
        /// <param name="type">type of payload to send</param>
        /// <param name="includeEvents">if <paramref name="type"/> is <code>replayed</code>, will the game output be populated</param>
        /// <param name="sharedSecret">shared secret to send</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        [HttpPost("test")]
        [Authorize]
        public async Task<ApiResponse> SendTest(
            [FromQuery] string url,
            [FromQuery] string type,
            [FromQuery] bool includeEvents,
            [FromQuery] string sharedSecret,
            CancellationToken cancel = default
        ) {
            if (_InstanceOptions.Value.EnableWebhooks == false) {
                return ApiForbidden($"webhooks are disabled by operator (hint: update the 'Instance' options in env.json)");
            }

            type = type.ToLower();

            AppAccount? currentUser = await _CurrentAccount.Get(cancel);
            if (currentUser == null) {
                return ApiInternalError($"no current user");
            }

            string? validatationError = _Validate(url, type, sharedSecret);
            if (validatationError != null) {
                return ApiBadRequest($"not sending test, validation error: {validatationError}");
            }

            List<BarMatch> recentMatches = await _MatchRepository.Search(new BarMatchSearchParameters() {
                ProcessingReplayed = true,
                OrderBy = OrderBy.START_TIME,
                OrderByDirection = OrderByDirection.DESC
            }, 0, 1, null, cancel);

            if (recentMatches.Count == 0) {
                return ApiInternalError($"no matches found that could be sent");
            }

            BarMatch recentMatch = recentMatches[0];

            Result<JsonObject, string> json = await _WebhookUtil.BuildBody(recentMatch.ID, type.ToLower() == "replayed", cancel);
            if (json.IsOk == false) {
                return ApiInternalError($"failed to generate JSON body to send [error={json.Error}]");
            }

            await _WebhookUtil.SendToWebhook(new MatchProcessingWebhook() {
                Url = url,
                Type = type.ToLower(),
                SharedSecret = sharedSecret,
                IncludeEvents = includeEvents,
            }, json.Value, cancel);

            return ApiOk();
        }

        /// <summary>
        ///     validate a set of properties for a webhook, returning null if no error, or a string indicating
        ///     what the error is
        /// </summary>
        /// <param name="url"></param>
        /// <param name="type"></param>
        /// <param name="sharedSecret"></param>
        /// <returns></returns>
        private string? _Validate(string url, string type, string sharedSecret) {
            type = type.ToLower();
            if (type != "parsed" && type != "replayed") {
                return $"{nameof(type)} must be 'parsed'|'replayed'";
            }

            if (sharedSecret.Length == 0) {
                return $"{nameof(sharedSecret)} has to be at least 1 character";
            }
            if (sharedSecret.Length > 256) {
                return $"{nameof(sharedSecret)} cannot be more than 256 characters";
            }

            if (url.Length > 1024) {
                return $"{nameof(url)} cannot be than 1024 characters";
            }

            if (Uri.TryCreate(url, new UriCreationOptions(), out Uri? result) == false || result == null) {
                string? hint = null;

                if (url.StartsWith("http") == false) {
                    hint = "include http(s)";
                }

                return $"{nameof(url)} '{url}' must be a valid URI{(hint != null ? $". hint: {hint}" : "")}";
            }

            if (_InstanceOptions.Value.EnableWebhookLoopbackUrl == false && result.IsLoopback == true) {
                return $"loopback URLs not allowed (hint: update the 'Instance' options in env.json to allow this";
            }

            return null;
        }

    }
}
