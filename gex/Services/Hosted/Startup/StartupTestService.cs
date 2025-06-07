using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Event;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.Startup {

    public class StartupTestService : BackgroundService {

        private readonly ILogger<StartupTestService> _Logger;
		private readonly BarMapParser _MapParser;
		private readonly BarDemofileParser _DemofileParser;

		public StartupTestService(ILogger<StartupTestService> logger,
			BarMapParser mapParser, BarDemofileParser demofileParser) {

			_Logger = logger;

			_MapParser = mapParser;
			_DemofileParser = demofileParser;
		}

		protected override Task ExecuteAsync(CancellationToken cancel) {
            return Task.Run(async () => {

				/*
				Result<BarMap, string> r = await _MapParser.Parse(@"F:\Gex\Engines\2025.04.01-win\maps\maps\eye_of_horus_1.7.1.sd7", cancel);
				if (r.IsOk == false) {
					_Logger.LogError($"failed to parse map: {r.Error}");
				}
				*/

				//byte[] input = await File.ReadAllBytesAsync("F:/Gex/Work/bomb.gzip", cancel);
				//await _DemofileParser.Parse("aaa", input, cancel);

				/*
				string[] files = Directory.GetFiles(@"F:\Gex\Engines\2025.04.01-win\maps\maps");
				foreach (string file in files) {
					if (file.EndsWith(".sd7") == false) {
						continue;
					}
					_Logger.LogInformation($"parsing map info [file={file}]");
					Result<BarMap, string> r2 = await _MapParser.Parse(file, cancel);
					if (r2.IsOk == false) {
						_Logger.LogError($"failed to parse map: {r2.Error}");
						break;
					}
				}
				*/
            }, cancel);
        }

    }
}
