using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

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

            host.MapPost("/webhook", async (HttpContext ctx) => {
                logger.LogInformation($"got webhook");
                string auth = ctx.Request.Headers.Authorization.FirstOrDefault()?.ToString() ?? "";

                using MemoryStream sr = new();
                await ctx.Request.Body.CopyToAsync(sr);
                string body = Encoding.UTF8.GetString(sr.ToArray());

                logger.LogInformation($"{body}");

                return Results.Ok();
            });

            host.Run();
        }

    }

    public class Secret {

        public string Url { get; set; } = "";

        public string Type { get; set; } = "";

        public string SharedSecret { get; set; } = "";

    }

}
