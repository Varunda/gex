using gex.Models;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarReplayFileApi {

        private readonly ILogger<BarReplayFileApi> _Logger;
		private readonly BarApiMetric _Metric;

        private static readonly HttpClient _Http = new HttpClient();

        private const string BAR_REPLAY_URL = "https://storage.uk.cloud.ovh.net/v1/AUTH_10286efc0d334efd917d476d7183232e/BAR/demos";

        static BarReplayFileApi() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

		public BarReplayFileApi(ILogger<BarReplayFileApi> logger,
			BarApiMetric metric) {

			_Logger = logger;
			_Metric = metric;
		}

		/// <summary>
		///     download a replay file from BAR servers
		/// </summary>
		/// <param name="fileName">name of the file to download</param>
		/// <param name="cancel">cancellation token</param>
		/// <returns>
		///     a <see cref="Result{T, E}"/>, which if <see cref="Result{T, E}.IsOk"/> is true, the <see cref="Result{T, E}.Value"/>
		///     will contain a <c>byte[]</c> containing the contents of the file from the BAR servers,
		///     or a string error
		/// </returns>
		public async Task<Result<byte[], string>> DownloadReplay(string fileName, CancellationToken cancel = default) {

            string url = BAR_REPLAY_URL + "/" + fileName;
            _Logger.LogDebug($"downloading replay file from BAR [filename={fileName}] [url={url}]");

            Stopwatch timer = Stopwatch.StartNew();
            HttpResponseMessage response = await _Http.GetAsync(url, cancel);
			_Metric.RecordDuration("download-replay", timer.ElapsedMilliseconds / 1000d);
			_Metric.RecordUse("download-replay");

            _Logger.LogDebug($"downloaded replay file from BAR [filename={fileName}] [url={url}] [duration={timer.ElapsedMilliseconds}ms]");

            if (response.IsSuccessStatusCode == false) {
                return $"expected 200 response, got {response.StatusCode} instead";
            }

            byte[] res = await response.Content.ReadAsByteArrayAsync(cancel);
            return res;
        }

    }
}
