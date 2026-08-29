
using gex.Common.Code;
using gex.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace gex.WebhookProxy {

    public class Program {

        public static void Main(string[] args) {
            WebApplicationOptions settings = new() {
                Args = args,
                ContentRootPath = Directory.GetCurrentDirectory()
            };

            WebApplicationBuilder builder = WebApplication.CreateBuilder(settings);
            builder.Logging.AddConsole(options => { options.FormatterName = "OneLineLogger"; })
                .AddConsoleFormatter<OneLineLogger, AppFormatterOptions>(options => { });

            builder.Configuration.AddJsonFile("secret.json");
            builder.Services.Configure<Secret>(builder.Configuration.GetSection("Secret"));

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            WebApplication host = builder.Build();

            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            HttpClient http = new();
            http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex-webhooks-proxy/0.1");
            http.Timeout = TimeSpan.FromSeconds(5);

            host.MapPost("/proxy", async (HttpContext httpContext, IOptions<Secret> secrets) => {
                if (httpContext.Request.Headers.TryGetValue("ProxySecret", out StringValues proxySecretValue) == false || proxySecretValue.First() == null) {
                    logger.LogDebug($"missing ProxySecret header [ip={httpContext.Connection.RemoteIpAddress}]");
                    return Results.StatusCode(403);
                }

                string proxySecret = proxySecretValue.First()!;
                if (proxySecret != secrets.Value.ProxySecret) {
                    logger.LogDebug($"wrong ProxySecret header [ip={httpContext.Connection.RemoteIpAddress}]");
                    return Results.StatusCode(403);
                }

                if (httpContext.Request.Query.TryGetValue("Target", out StringValues targetValue) == false || targetValue.First() == null) {
                    logger.LogDebug($"missing Target query parameter");
                    return Results.BadRequest($"missing target in query");
                }

                if (httpContext.Request.Headers.Authorization.First() == null) {
                    logger.LogDebug("missing Authorization header");
                    return Results.BadRequest($"missing Authorization header");
                }

                try {
                    Stopwatch timer = Stopwatch.StartNew();
                    string target = targetValue.First()!;

                    using MemoryStream sr = new();
                    await httpContext.Request.Body.CopyToAsync(sr);
                    string body = Encoding.UTF8.GetString(sr.ToArray());

                    HttpRequestMessage req = new(HttpMethod.Post, target);
                    string auth = httpContext.Request.Headers.Authorization.First()!.Split(" ")[1];
                    req.Headers.Authorization = new AuthenticationHeaderValue("SharedSecret", auth);
                    req.Content = new StringContent(body);

                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                    await http.SendAsync(req, cts.Token);

                    logger.LogInformation($"proxyied webhook [target={target}] [timer={timer.ElapsedMilliseconds}ms] [size={body.Length}]"); 
                } catch (Exception ex) {
                    logger.LogError(ex, $"failed to send webhook to target [target={targetValue}]");
                    return Results.InternalServerError(ex.Message);
                }

                return Results.Ok();
            });

            host.Run();
        }

        public class Secret {

            public string ProxySecret { get; set; } = "";

        }

    }
}
