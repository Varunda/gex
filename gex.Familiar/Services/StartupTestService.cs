using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Familiar.Services {

    public class StartupTestService : IHostedService {

        private readonly ILogger<StartupTestService> _Logger;
        private readonly MatchUploader _Uploader;

        public StartupTestService(ILogger<StartupTestService> logger,
            MatchUploader uploader) {

            _Logger = logger;
            _Uploader = uploader;
        }

        public Task StartAsync(CancellationToken cancellationToken) {

            Task.Run(async () => {
                try {
                    await Run(cancellationToken);
                } catch (Exception ex) {
                    _Logger.LogError(ex, "guh");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        private async Task Run(CancellationToken cancel) {
            string dir = "F:\\GexFamiliar\\Engines\\2025.04.08-win\\data-2013137204";
            byte[] demofile = await File.ReadAllBytesAsync(Path.Join(dir, "demos", "2025-10-30_20-12-44-994_Comet Catcher Remake 1_2025.04.08.sdfz"));
            byte[] actions = await File.ReadAllBytesAsync(Path.Join(dir, "actions.json"));
            byte[] stdout = await File.ReadAllBytesAsync(Path.Join(dir, "infolog.txt"));
            byte[] stderr = { 0x00 };

            _Logger.LogDebug($"yo i'm doing the thing haha");
            await _Uploader.Upload(demofile, actions, stdout, stderr);
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
