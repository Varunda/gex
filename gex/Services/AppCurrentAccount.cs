using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using gex.Models;
using gex.Services.Db;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services {

    /// <summary>
    ///     Service to get the current user making an HTTP request
    /// </summary>
    public class AppCurrentAccount {

        private readonly ILogger<AppCurrentAccount> _Logger;
        private readonly IHttpContextAccessor _Context;
        private readonly AppAccountDbStore _AccountDb;

        public AppCurrentAccount(ILogger<AppCurrentAccount> logger,
            IHttpContextAccessor context, AppAccountDbStore accountDb) {

            _Logger = logger;
            _Context = context;
            _AccountDb = accountDb;
        }

        /// <summary>
        ///     Get the current user based who a <see cref="BaseContext"/>
        /// </summary>
        /// <param name="ctx">Context of the application command</param>
        /// <returns>
        ///     Null if the field <see cref="BaseContext.Member"/> is null, or null if the user doesn't have an account
        /// </returns>
        public async Task<AppAccount?> GetDiscord(BaseContext ctx) {
            DiscordMember? caller = ctx.Member;
            if (caller == null) {
                return null;
            }

            AppAccount? account = await _AccountDb.GetByDiscordID(caller.Id, CancellationToken.None);

            return account;
        }

        public Task<ulong?> GetDiscordID() {
            if (_Context.HttpContext == null) {
                _Logger.LogWarning($"_Context.HttpContext is null, cannot get claims");
                return Task.FromResult((ulong?)null);
            }

            HttpContext httpContext = _Context.HttpContext;

            if (httpContext.User.Identity == null) {
                _Logger.LogWarning($"httpContext.User.Identity is null");
                return Task.FromResult((ulong?)null);
            }

            if (httpContext.User.Identity.IsAuthenticated == false) {
                return Task.FromResult((ulong?)null);
            } else if (httpContext.User is ClaimsPrincipal claims) {
                /*
                string s = "";
                foreach (Claim claim in claims.Claims) {
                    s += $"{claim.Type} = {claim.Value};";
                }
                _Logger.LogDebug($"{s}");
                */

                Claim? idClaim = claims.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim == null || string.IsNullOrEmpty(idClaim.Value)) {
                    return Task.FromResult((ulong?)null);
                }

                string id = idClaim.Value;

                if (ulong.TryParse(id, out ulong discordID) == false) {
                    throw new InvalidCastException($"failed to convert {id} to a valid ulong");
                }

                return Task.FromResult((ulong?)discordID);
            } else {
                _Logger.LogWarning($"Unchecked stat of httpContext.User");
            }

            return Task.FromResult((ulong?)null);
        }

        /// <summary>
        ///     Get the current user, null if the user is not signed in
        /// </summary>
        /// <returns></returns>
        public async Task<AppAccount?> Get() {
            if (_Context.HttpContext == null) {
                _Logger.LogWarning($"_Context.HttpContext is null, cannot get claims");
                return null;
            }

            HttpContext httpContext = _Context.HttpContext;

            if (httpContext.User.Identity == null) {
                _Logger.LogWarning($"httpContext.User.Identity is null");
                return null;
            }

            if (httpContext.User.Identity.IsAuthenticated == false) {
                return null;
            } else if (httpContext.User is ClaimsPrincipal claims) {
                /*
                string s = "";
                foreach (Claim claim in claims.Claims) {
                    s += $"{claim.Type} = {claim.Value};";
                }
                _Logger.LogDebug($"{s}");
                */

                Claim? idClaim = claims.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim == null || string.IsNullOrEmpty(idClaim.Value)) {
                    return null;
                }

                string id = idClaim.Value;

                if (ulong.TryParse(id, out ulong discordID) == false) {
                    throw new InvalidCastException($"failed to convert {id} to a valid ulong");
                }

                return await _AccountDb.GetByDiscordID(discordID, CancellationToken.None);
            } else {
                _Logger.LogWarning($"Unchecked stat of httpContext.User");
            }

            return null;
        }

    }
}
