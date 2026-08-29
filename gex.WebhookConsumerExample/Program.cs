using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace gex.WebhookConsumerExample {

    public class Program {

        public static void Main(string[] args) {
            WebApplicationOptions settings = new() {
                Args = args,
                ContentRootPath = Directory.GetCurrentDirectory()
            };

            WebApplicationBuilder builder = WebApplication.CreateBuilder(settings);
            builder.Configuration.AddJsonFile("secret.json");
            builder.Services.Configure<Secret>(builder.Configuration.GetSection("Secret"));

            using WebApplication host = builder.Build();
            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            host.MapPost("/webhook", async (HttpContext ctx, IOptions<Secret> secrets) => {
                logger.LogInformation($"got webhook");
                string auth = ctx.Request.Headers.Authorization.FirstOrDefault()?.ToString() ?? "";
                if (("SharedSecret " + secrets.Value.SharedSecret) != auth) {
                    logger.LogWarning($"wrong auth header [value={auth}]");
                    return Results.Ok();
                }

                using MemoryStream sr = new();
                await ctx.Request.Body.CopyToAsync(sr);
                string body = Encoding.UTF8.GetString(sr.ToArray());

                JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
                JsonElement match = json.GetProperty("match");
                logger.LogInformation($"got webhook for match [gameID={match.GetProperty("id").GetString()}]");

                return Results.Ok();
            });

            host.Run();
        }

    }

    public class Secret {

        public string SharedSecret { get; set; } = "";

    }

}
