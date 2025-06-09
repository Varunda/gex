using gex.Models;
using gex.Models.Internal;
using gex.Services;
using gex.Services.Db.Account;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code {

    /// <summary>
    ///     Attribute to add to actions to require a user to have a <see cref="AppAccount"/>,
    ///     and that account has the necessary permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class PermissionNeededAttribute : TypeFilterAttribute {

        public PermissionNeededAttribute(params string[] perms) : base(typeof(PermissionNeededFilter)) {
            Arguments = new object[] { perms };
        }

    }

    public class PermissionNeededFilter : IAsyncAuthorizationFilter {

        private readonly ILogger<PermissionNeededFilter> _Logger;
        private readonly IHttpContextAccessor _Context;
        private readonly AppAccountDbStore _AppAccountDb;
        private readonly AppPermissionRepository _PermissionRepository;
        private readonly AppCurrentAccount _CurrentAccount;

        public readonly List<string> Permissions;

        public PermissionNeededFilter(ILogger<PermissionNeededFilter> logger,
            IHttpContextAccessor context, AppAccountDbStore appAccountDb,
            AppPermissionRepository permissionRepository, AppCurrentAccount currentAccount,
            string[] perms) {

            Permissions = perms.ToList();

            _Logger = logger;
            _Context = context;
            _AppAccountDb = appAccountDb;
            _PermissionRepository = permissionRepository;
            _CurrentAccount = currentAccount;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {

            AppAccount? account = await _CurrentAccount.Get();

            Stopwatch timer = Stopwatch.StartNew();
            bool hasPerm = await checkPermission(context, account, context.HttpContext.RequestAborted);
            long timerMs = timer.ElapsedMilliseconds;
            _Logger.LogDebug($"user permission check done [timer={timerMs}ms] [granted={hasPerm}] [account={account?.ID}/{account?.Name}] "
                + $"[Permissions={string.Join(", ", Permissions)}] [url={context.HttpContext.Request.Path.Value}]");

            if (hasPerm == false) {
                if (context.HttpContext.Response.HasStarted == true) {
                    _Logger.LogError($"response started, cannot set 403Forbidden");
                    throw new ApplicationException($"cannot forbid access to action, response has started");
                }

                if (context.HttpContext.Request.Path.StartsWithSegments("/api") == true) {
                    context.Result = new ApiResponse(403, new Dictionary<string, string>() {
                        { "error", "no permission" }
                    });
                } else {
                    context.Result = new RedirectToActionResult("Unauthorized", "Home", null);
                }
            }

        }

        private async Task<bool> checkPermission(AuthorizationFilterContext context, AppAccount? account, CancellationToken cancel) {
            if (account == null) {
                _Logger.LogTrace($"user was authed, but does not have an account [url={context.HttpContext.Request.Path.Value}]");

                return false;
            }

            _Logger.LogTrace($"checking if user has permission [account={account.ID}/{account.Name}] [Permissions={string.Join(", ", Permissions)}] "
                + $"[url={context.HttpContext.Request.Path.Value}]");

            // account 1 is system user, account 2 is the first user made
            if (account.ID <= 2) {
                _Logger.LogTrace($"user has permission as they are the owner [account={account.Name}] [url={context.HttpContext.Request.Path.Value}]");
                return true;
            }

            HashSet<string> accountPerms = new();

            Stopwatch timer = Stopwatch.StartNew();
            List<AppGroupPermission> perms = await _PermissionRepository.GetByAccountID(account.ID, cancel);
            foreach (AppGroupPermission perm in perms) {
                accountPerms.Add(perm.Permission.ToLower());
            }
            _Logger.LogTrace($"loaded user permissions [perms.Count={perms.Count}] [timer={timer.ElapsedMilliseconds}ms] [account={account.ID}/{account.Name}]");

            bool hasPerm = false;
            foreach (string perm in Permissions) {
                if (accountPerms.Contains(perm.ToLower())) {
                    hasPerm = true;
                    break;
                }
            }

            return hasPerm;
        }


    }
}
