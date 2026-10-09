using gex.Coven.Services.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Hosted {

    public class HostedGexLuaStartup : BackgroundService {

        private readonly ILogger<HostedGexLuaStartup> _Logger;

        private static readonly HttpClient _Http = new();

        static HostedGexLuaStartup() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex.Coven/0.1");
        }

        public HostedGexLuaStartup(ILogger<HostedGexLuaStartup> logger) {
            _Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"downloading gex.lua and BYAR.lua");

            Directory.CreateDirectory(Path.Join(ShellUtil.GetWorkingDirectory(), "cache"));
            await DownloadFile("gex.lua", stoppingToken);
            await DownloadFile("BYAR.lua", stoppingToken);
        }

        private async Task DownloadFile(string file, CancellationToken cancel) {
            HttpResponseMessage res = await _Http.GetAsync($"https://gex.honu.pw/{file}", cancel);
            if (res.IsSuccessStatusCode == false) {
                _Logger.LogError($"failed to download file [status={res.StatusCode}] [file={file}]");
                return;
            }

            string outputPath = Path.Join(ShellUtil.GetWorkingDirectory(), "cache", file);

            using FileStream widgetOutput = File.OpenWrite(outputPath);
            await res.Content.CopyToAsync(widgetOutput, cancel);

            if (File.Exists(outputPath) == false) {
                _Logger.LogError($"failed to find file after download [file={file}] [path={outputPath}]");
            }
        }

    }
}
