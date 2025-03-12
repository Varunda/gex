using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using gex.Services;
using gex.Services.Hosted;
using gex.Services.Db;
using gex.Services.Db.Implementations;
using gex.Services.Repositories;
using System.Text.Json;
using gex.Models;
using gex.Services.Hosted.Startup;
using gex.Code.Converters;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using gex.Code;
using NReco.Logging.File;
using Microsoft.Extensions.Caching.Memory;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Globalization;
using gex.Services.Db.Readers;
using gex.Models.Options;
using gex.Services.BarApi;
using gex.Services.Demofile;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Dapper.ColumnMapper;
using Dapper;

namespace gex {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            string stuff = ((IConfigurationRoot)Configuration).GetDebugView();
            Console.WriteLine(stuff);

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
                config.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
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

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(options => {
                options.Cookie.Name = "gex-auth";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.ForwardChallenge = DiscordAuthenticationDefaults.AuthenticationScheme;
            }).AddDiscord(options => {
                DiscordOptions? dOpts = Configuration.GetSection("Discord").Get<DiscordOptions>();
                if (dOpts == null) {
                    throw new InvalidOperationException($"no discord configuration in the Discord: section configured");
                }

                if (string.IsNullOrWhiteSpace(dOpts.ClientId)) {
                    throw new InvalidOperationException($"missing ClientId. did you set Discord:ClientId?");
                }
                if (string.IsNullOrWhiteSpace(dOpts.ClientSecret)) {
                    throw new InvalidOperationException($"missing ClientSecret. did you set Discord:ClientSecret?");
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

            services.Configure<DiscordOptions>(Configuration.GetSection("Discord"));
            services.Configure<InstanceOptions>(Configuration.GetSection("Instance"));
            services.Configure<HttpOptions>(Configuration.GetSection("Http"));
            services.Configure<FileStorageOptions>(Configuration.GetSection("FileStorage"));

            services.AddSingleton<ServiceHealthMonitor>();

            services.AddTransient<HttpUtilService>();
            services.AddSingleton<InstanceInfo>();

            services.AddTransient<IActionResultExecutor<ApiResponse>, ApiResponseExecutor>();
            services.AddSingleton<IDbHelper, DbHelper>();
            services.AddSingleton<IDbCreator, DefaultDbCreator>();

            services.AddSingleton<CommandBus, CommandBus>();

            services.AddGexQueueServices(); // queue services

            services.AddDatabasesServices(); // Db services
            services.AddAppDatabaseReadersServices(); // DB readers
            services.AddBarApiServices();
            services.AddGexRepositories();
            services.AddSingleton<PathEnvironmentService>();
            services.AddSingleton<BarDemofileParser>();

            // Hosted services
            services.AddHostedService<DbCreatorStartupService>(); // Have first to ensure DBs exist
            services.AddAppStartupServices(); // startup services
            services.AddQueueProcessors(); // Hosted queues
            services.AddPeriodicServices(); // periodic run services

            if (Configuration.GetValue<bool>("Discord:Enabled") == true) {
                services.AddSingleton<DiscordWrapper>();
                services.AddHostedService<DiscordService>();
            }

            services.AddTransient<AppCurrentAccount>();

            services.Configure<ForwardedHeadersOptions>(options => {
                // look for the x-forwarded-for headers to know the remote IP
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // needed for Gex on production, which is behind Nginx, will accept the Cookie for Google OAuth2 
                options.KnownProxies.Add(IPAddress.Parse("64.227.19.86"));
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

            app.UseSwagger(doc => { });
            app.UseSwaggerUI(doc => {
                doc.SwaggerEndpoint("/swagger/api/swagger.json", "api");
                doc.RoutePrefix = "api-doc";
                doc.DocumentTitle = "API documentation";
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors();

            app.UseMiddleware<TimerMiddleware>();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "unauth",
                    pattern: "/unauthorized/{*.}",
                    defaults: new { controller = "Unauthorized", action = "Index"}
                );

                endpoints.MapControllerRoute(
                    name: "index",
                    pattern: "/{action}",
                    defaults: new { controller = "Home", action = "Index" }
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

                endpoints.MapSwagger();
            });

            logger.LogInformation($"pipeline configured");
        }


    }
}
