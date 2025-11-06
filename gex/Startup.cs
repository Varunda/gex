using AspNet.Security.OAuth.Discord;
using gex.Code;
using gex.Code.Converters;
using gex.Code.Hubs;
using gex.Code.Hubs.Implementations;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Common.Services;
using gex.Models;
using gex.Models.Options;
using gex.Services;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Db.Implementations;
using gex.Services.Db.Readers;
using gex.Services.Discord;
using gex.Services.Hosted;
using gex.Services.Hosted.Startup;
using gex.Services.Lobby;
using gex.Services.Metrics;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Storage;
using gex.Services.Util;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using NReco.Logging.File;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace gex {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            string stuff = ((IConfigurationRoot)Configuration).GetDebugView();
            //Console.WriteLine(stuff);

            services.AddLogging(builder => {
                builder.AddFile("logs/app-{0:yyyy}-{0:MM}-{0:dd}.log", options => {
                    options.FormatLogFileName = fName => {
                        return string.Format(fName, DateTime.UtcNow);
                    };
                });
            });

            services.AddRouting();

            services.AddSignalR(options => {
                options.EnableDetailedErrors = true;
            }).AddJsonProtocol(options => {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddMvc(options => {

            }).AddJsonOptions(config => {
                // don't forget to update these in ApiResponse as well!!!!!!!!!!!!!!!!!!!!!
                config.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                config.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
                config.JsonSerializerOptions.Converters.Add(new Vector3JsonConverter());
            }).AddRazorRuntimeCompilation();

            services.AddSwaggerGen(doc => {
                doc.SwaggerDoc("api", new OpenApiInfo() { Title = "API", Version = "v0.1" });

                Console.Write("Including XML documentation in: ");
                foreach (string file in Directory.GetFiles(AppContext.BaseDirectory, "*.xml")) {
                    Console.Write($"{Path.GetFileName(file)} ");
                    doc.IncludeXmlComments(file);
                }
                Console.WriteLine("");
            });

            if (Configuration.GetValue<bool>("Instance:LocalhostDeveloperAccount") == true) {
                services.AddAuthentication(options => {
                    options.DefaultAuthenticateScheme = LocalhostDeveloperAuthenticationDefaults.AuthenticationScheme;
                }).AddScheme<LocalhostDeveloperAuthenticationOptions, LocalhostDeveloperAuthentication>(
                    LocalhostDeveloperAuthenticationDefaults.AuthenticationScheme,
                    options => { }
                );
            } else {

                bool usingJwtAuth = Configuration.GetValue<bool>("Jwt:Enabled");

                AuthenticationBuilder authBuilder = services.AddAuthentication(options => {
                    options.DefaultScheme = "gex-policy-auth";
                    /*
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    */
                    options.DefaultChallengeScheme = "gex-policy-auth";
                }).AddCookie(options => {
                    options.Cookie.Name = "gex-auth";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;

                    options.ForwardChallenge = DiscordAuthenticationDefaults.AuthenticationScheme;
                });

                if (usingJwtAuth == true) {
                    authBuilder.AddJwtBearer((JwtBearerOptions options) => {
                        options.ForwardChallenge = CookieAuthenticationDefaults.AuthenticationScheme;

                        options.Authority = Configuration.GetValue<string>("Jwt:Authority")
                            ?? throw new Exception($"if Jwt:Enabled is true, then Jwt:Authority must be given as well");

                        options.Events = new JwtBearerEvents() {
                            OnMessageReceived = (MessageReceivedContext ctx) => {
                                StringValues accessToken = ctx.Request.Query["access_token"];
                                PathString path = ctx.HttpContext.Request.Path;

                                // only use JWT tokens for auth to the familiar hub
                                if (string.IsNullOrEmpty(accessToken) == false 
                                    && path.StartsWithSegments("/ws/familiar")) {

                                    ctx.Token = accessToken;
                                }

                                return Task.CompletedTask;
                            }
                        };
                    });
                }

                if (Configuration.GetValue<bool>("Discord:Enabled") == true) {
                    authBuilder.AddDiscord(options => {
                        DiscordOptions? dOpts = Configuration.GetSection("Discord").Get<DiscordOptions>();
                        if (dOpts == null) {
                            throw new InvalidOperationException($"no discord configuration in the Discord: section configured");
                        }

                        if (string.IsNullOrWhiteSpace(dOpts.ClientId)) {
                            throw new InvalidOperationException($"missing ClientId. is Discord:ClientId set?");
                        }
                        if (string.IsNullOrWhiteSpace(dOpts.ClientSecret)) {
                            throw new InvalidOperationException($"missing ClientSecret. is Discord:ClientSecret set?");
                        }

                        options.ClientId = dOpts.ClientId;
                        options.ClientSecret = dOpts.ClientSecret;

                        options.CallbackPath = "/auth/callback"; // configured callback

                        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                        options.SaveTokens = true;

                        // map the returned JSON from Discord to auth claims
                        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                        options.ClaimActions.MapCustomJson("urn:discord:avatar:url",
                            user => string.Format(CultureInfo.InvariantCulture, "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
                            user.GetString("id"),
                            user.GetString("avatar"),
                            user.GetString("avatar")!.StartsWith("a_") ? "gif" : "png")
                        );

                        options.Scope.Add("identify");
                    });
                }

                // policy scheme that decides to use the JWT auth or cookie auth by default
                // JWT auth is used only if the authorization is given and it's on the familiar hub,
                // otherwise cookie auth it is
                authBuilder.AddPolicyScheme("gex-policy-auth", displayName: null, options => {
                    options.ForwardDefaultSelector = (HttpContext ctx) => {
                        StringValues accessToken = ctx.Request.Query["access_token"];
                        if (string.IsNullOrEmpty(accessToken)) {
                            accessToken = ctx.Request.Headers.Authorization;
                        }

                        PathString path = ctx.Request.Path;

                        // only use JWT tokens for auth to the familiar hub
                        if (string.IsNullOrEmpty(accessToken) == false 
                            && (path.StartsWithSegments("/ws/familiar") || path.StartsWithSegments("/api/match-upload/upload-familiar"))) {

                            return JwtBearerDefaults.AuthenticationScheme;
                        }

                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                });
            }

            // require all endpoints to be authorized unless another policy is defined
            /*
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(DiscordAuthenticationDefaults.AuthenticationScheme)
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());
            */

            services.AddRazorPages();
            services.AddSingleton<IMemoryCache, AppCache>();
            services.AddHttpContextAccessor();

            services.AddCors(o => o.AddDefaultPolicy(builder => {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
            }));

            services.AddRateLimiter(opt => {

                // two rate limits exist,
                // one prevents one client from making a bunch of requests at once,
                // the other limits how many api requests per second a client can make
                opt.GlobalLimiter = PartitionedRateLimiter.CreateChained(

                    // only allow 10 concurrent request at once
                    PartitionedRateLimiter.Create<HttpContext, IPAddress>(context => {
                        // non-api endpoints have 0 rate limits
                        if (!context.Request.Path.StartsWithSegments("/api")) {
                            return RateLimitPartition.GetNoLimiter(IPAddress.None);
                        }

                        IPAddress addr = context.Connection.RemoteIpAddress ?? throw new Exception($"missing ip addr");

                        return RateLimitPartition.GetConcurrencyLimiter(addr, _ => {
                            return new ConcurrencyLimiterOptions() {
                                PermitLimit = 10,
                                QueueLimit = 10
                            };
                        });
                    }),

                    // 
                    PartitionedRateLimiter.Create<HttpContext, IPAddress>(context => {
                        // rate limit api and ws connections
                        if (!context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/ws")) {
                            return RateLimitPartition.GetNoLimiter(IPAddress.None);
                        }

                        IPAddress addr = context.Connection.RemoteIpAddress ?? throw new Exception($"missing ip addr");

                        return RateLimitPartition.GetTokenBucketLimiter(addr, _ => {
                            return new TokenBucketRateLimiterOptions() {
                                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                                TokenLimit = 300,
                                AutoReplenishment = true,
                                TokensPerPeriod = 60
                            };
                        });
                    })
                );

                opt.OnRejected = (context, cancel) => {
                    ILogger? logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                        .CreateLogger("gex.Startup.Ratelimiter");

                    logger?.LogInformation($"rate limit hit [ip={context.HttpContext.Connection.RemoteIpAddress}] [url='{context.HttpContext.Request.Path}'] "
                        + $"[referrer='{context.HttpContext.Request.Headers.Referer}']");

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter.Name, out object? retryAfter)) {
                        if (retryAfter is TimeSpan ts) {
                            context.HttpContext.Response.Headers.RetryAfter = $"{ts.TotalSeconds}";
                        }
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    // not setting this throws an XML parse error in firefox
                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.WriteAsync("{ \"error\": \"too many requests!\" }", cancel);

                    return ValueTask.CompletedTask;
                };
            });

            services.Configure<DiscordOptions>(Configuration.GetSection("Discord"));
            services.Configure<InstanceOptions>(Configuration.GetSection("Instance"));
            services.Configure<HttpOptions>(Configuration.GetSection("Http"));
            services.Configure<FileStorageOptions>(Configuration.GetSection("FileStorage"));
            services.Configure<ServiceOptions>(Configuration.GetSection("Services"));
            services.Configure<SpringLobbyOptions>(Configuration.GetSection("Spring"));
            services.Configure<GitHubOptions>(Configuration.GetSection("GitHub"));

            services.AddSingleton<ServiceHealthMonitor>();

            services.AddTransient<HttpUtilService>();
            services.AddSingleton<InstanceInfo>();
            services.AddTransient<EnginePathUtil>();

            services.AddTransient<IActionResultExecutor<ApiResponse>, ApiResponseExecutor>();
            services.AddSingleton<IDbHelper, DbHelper>();
            services.AddSingleton<IDbCreator, DefaultDbCreator>();

            services.AddSingleton<CommandBus, CommandBus>();

            services.AddGexQueueServices(); // queue services

            services.AddDatabasesServices(); // Db services
            services.AddAppDatabaseReadersServices(); // DB readers
            services.AddBarApiServices();
            services.AddGexRepositories();
            services.AddGexParsers();
            services.AddSingleton<LuaRunner>();
            services.AddStorageServices();

            services.AddSingleton<PathEnvironmentService>();
            services.AddSingleton<BarMatchTitleUtilService>();
            services.AddSingleton<ApmCalculatorUtil>();

            // Hosted services
            services.AddHostedService<DbCreatorStartupService>(); // Have first to ensure DBs exist
            services.AddAppStartupServices(); // startup services
            services.AddQueueProcessors(); // Hosted queues
            services.AddPeriodicServices(); // periodic run services
            services.AddBackgroundServices();
            services.AddGexMetrics();
            services.AddSingleton<BarMatchPriorityCalculator>();
            services.AddSingleton<BarDemofileResultProcessor>();
            services.AddLobbyServices(enabled: Configuration.GetValue<bool>("Spring:Enabled"));

            if (Configuration.GetValue<bool>("Discord:Enabled") == true) {
                services.AddSingleton<DiscordWrapper>();
                services.AddHostedService<DiscordService>();
            }

            if (Configuration.GetValue<bool>("Instance:LocalhostDeveloperAccount") == true) {
                services.AddTransient<ICurrentAccount, LocalhostCurrentAccount>();
            } else {
                services.AddTransient<ICurrentAccount, AppCurrentAccount>();
            }

            services.Configure<ForwardedHeadersOptions>(options => {
                // look for the x-forwarded-for headers to know the remote IP
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 3; // behind 2 proxies, cloudflare and nginx

                // from https://www.cloudflare.com/ips/
                List<string> cfips = [
                    "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22", "141.101.64.0/18",
                    "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20", "197.234.240.0/22", "198.41.128.0/17",
                    "162.158.0.0/15", "104.16.0.0/13", "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22",

                    "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32",
                    "2405:8100::/32", "2a06:98c0::/29", "2c0f:f248::/32"
                ];

                foreach (string s in cfips) {
                    options.KnownNetworks.Add(Microsoft.AspNetCore.HttpOverrides.IPNetwork.Parse(s));
                }

                // add any additional proxies used
                List<string> proxies = Configuration.GetValue<List<string>>("Instance:Proxies") ?? [];
                foreach (string proxy in proxies) {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });

            services.AddHostedService<StartupTestService>();

            Console.WriteLine($"!!!!! ConfigureServices finished !!!!!");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IHostApplicationLifetime lifetime, ILogger<Startup> logger) {

            app.UseForwardedHeaders();
            app.UseMiddleware<ExceptionHandlerMiddleware>();

            logger.LogInformation($"environment: {env.EnvironmentName}");

            app.UseStaticFiles();
            app.UseRouting();
            app.UseRateLimiter();

            app.UseSwagger(doc => { });
            app.UseSwaggerUI(doc => {
                doc.SwaggerEndpoint("/swagger/api/swagger.json", "api");
                doc.RoutePrefix = "api-doc";
                doc.DocumentTitle = "API documentation";
            });

            app.UseCors();

            app.UseResponseCaching();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<TimerMiddleware>();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "unauth",
                    pattern: "/unauthorized/{*.}",
                    defaults: new { controller = "Unauthorized", action = "Index" }
                );

                endpoints.MapControllerRoute(
                    name: "index",
                    pattern: "/{action}",
                    defaults: new { controller = "Home", action = "Index" }
                );

                endpoints.MapControllerRoute(
                    name: "image-proxy",
                    pattern: "/image-proxy/{action}/{*.}",
                    defaults: new { controller = "GameImage" }
                );

                endpoints.MapControllerRoute(
                    name: "accountmanagement",
                    pattern: "/accountmanagement/{*.}",
                    defaults: new { controller = "Home", action = "AccountManagement" }
                );

                endpoints.MapControllerRoute(
                    name: "api",
                    pattern: "/api/{controller}/{action}"
                );

                endpoints.MapControllerRoute(
                    name: "match",
                    pattern: "/match/{gameID}",
                    defaults: new { controller = "Home", action = "Match" }
                );

                endpoints.MapControllerRoute(
                    name: "user",
                    pattern: "/user/{userID}",
                    defaults: new { controller = "Home", action = "User" }
                );

                endpoints.MapControllerRoute(
                    name: "map",
                    pattern: "/map/{*.}",
                    defaults: new { controller = "Home", action = "Map" }
                );

                endpoints.MapControllerRoute(
                    name: "unit",
                    pattern: "/unit/{*.}",
                    defaults: new { controller = "Home", action = "Unit" }
                );

                endpoints.MapControllerRoute(
                    name: "pool",
                    pattern: "/pool/{poolID}",
                    defaults: new { controller = "Home", action = "Pool" }
                );

                endpoints.MapControllerRoute(
                    name: "mapname",
                    pattern: "/mapname/{mapName}",
                    defaults: new { controller = "Home", action = "MapName" }
                );

                endpoints.MapControllerRoute(
                    name: "download match",
                    pattern: "/downloadmatch/{gameID}",
                    defaults: new { controller = "Home", action = "DownloadMatch" }
                );

                endpoints.MapHub<HeadlessReplayHub>("/ws/headless-run").DisableHttpMetrics();
                endpoints.MapHub<FamiliarHub>("/ws/familiar").DisableHttpMetrics();

                endpoints.MapSwagger();
            });

            logger.LogInformation($"pipeline configured");
        }

    }
}
