using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using gex.Models;
using gex.Services.Db;
using gex.Services.Repositories;
using gex.Services;
using System.Diagnostics;

namespace gex.Code {

    /// <summary>
    ///     Attribute to add to actions to require a user to have a <see cref="AppAccount"/>,
    ///     and that account has the necessary permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AccountRequiredAttribute : TypeFilterAttribute {

        public AccountRequiredAttribute(params string[] perms) : base(typeof(PermissionNeededFilter)) {
            Arguments = new object[] { perms };
        }

    }

    public class PermissionNeededFilter : IAsyncAuthorizationFilter {

        private readonly ILogger<PermissionNeededFilter> _Logger;
        private readonly IHttpContextAccessor _Context;
        private readonly AppAccountDbStore _AppAccountDb;
        private readonly AppCurrentAccount _CurrentAccount;

        public readonly List<string> Permissions;

        public PermissionNeededFilter(ILogger<PermissionNeededFilter> logger,
            IHttpContextAccessor context, AppAccountDbStore appAccountDb,
            AppCurrentAccount currentAccount,
            string[] perms) { 

            Permissions = perms.ToList();

            _Logger = logger;
            _Context = context;
            _AppAccountDb = appAccountDb;
            _CurrentAccount = currentAccount;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
            AppAccount? account = await _CurrentAccount.Get();

            bool hasPerm = account != null;

            if (hasPerm == false) {

                ulong? discordID = await _CurrentAccount.GetDiscordID();
                _Logger.LogInformation($"unauthorized user attempted to access resource [discordID={discordID}] [url={context.HttpContext.Request.Path}]");

                if (context.HttpContext.Response.HasStarted == true) {
                    _Logger.LogError($"response started, cannot set 403Forbidden");
                    throw new ApplicationException($"cannot forbid access to action, response has started");
                }

                if (context.HttpContext.Request.Path.StartsWithSegments("/api") == true) {
                    context.Result = new ApiResponse(403, new Dictionary<string, string>() {
                        { "error", "no permission" }
                    });
                } else {
                    context.Result = new RedirectToActionResult("Index", "Unauthorized", null);
                }
            }

        }

    }

}
