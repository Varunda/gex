using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace gex.Code {

    public class LocalhostDeveloperAuthentication : AuthenticationHandler<LocalhostDeveloperAuthenticationOptions> {

        private readonly ILogger<LocalhostDeveloperAuthentication> _Logger;

        private static DateTime _LastReminder = DateTime.MinValue;

        public LocalhostDeveloperAuthentication(IOptionsMonitor<LocalhostDeveloperAuthenticationOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) {

            _Logger = logger.CreateLogger<LocalhostDeveloperAuthentication>();
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            if (DateTime.UtcNow - _LastReminder >= TimeSpan.FromMinutes(3)) {
                _Logger.LogInformation($"reminder: using the localhost developer authentication, all requests will be authorized (3m cooldown)");
                _LastReminder = DateTime.UtcNow;
            }

            Claim[] claims = [
                new Claim(ClaimTypes.Name, "localhost dev"),
                new Claim(ClaimTypes.NameIdentifier, "0") // usually this would be the discord ID
            ];

            ClaimsIdentity ident = new(claims, LocalhostDeveloperAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal princ = new(ident);
            AuthenticationTicket ticket = new(princ, LocalhostDeveloperAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

    }

    public static class LocalhostDeveloperAuthenticationDefaults {

        public const string AuthenticationScheme = "localhost-developer-authentication";

    }

    public class LocalhostDeveloperAuthenticationOptions : AuthenticationSchemeOptions {

    }

}
