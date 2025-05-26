using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarMapApi {

        private readonly ILogger<BarMapApi> _Logger;
		private readonly BarApiMetric _Metric;

        private const string BASE_URL = "https://api.bar-rts.com/maps";

        private static readonly HttpClient _Http = new HttpClient();
        static BarMapApi() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

		public BarMapApi(ILogger<BarMapApi> logger,
			BarApiMetric metric) {

			_Logger = logger;
			_Metric = metric;
		}

		/// <summary>
		///     load a <see cref="BarMap"/> from the BAR api
		/// </summary>
		/// <param name="filename">name of the map. this is normalized to replace spaces with underscores</param>
		/// <param name="cancel">cancellation token</param>
		/// <returns>
		///     a <see cref="Result{T, E}"/> that indicates the success of loading a <see cref="BarMap"/>
		///     from the BAR api
		/// </returns>
		public async Task<Result<BarMap, string>> GetByName(string filename, CancellationToken cancel) {

            string url = BASE_URL + "/" + filename;
            _Logger.LogTrace($"attempting map load [filename={filename}] [url={url}]");
			Stopwatch timer = Stopwatch.StartNew();

            HttpResponseMessage response = await _Http.GetAsync(url);

			double durationSec = timer.ElapsedMilliseconds / 1000d;
			_Metric.RecordUse("map");
			_Metric.RecordDuration("map", durationSec);

            if (response.IsSuccessStatusCode == false) {
                return $"failed to call bar API [status code={response.StatusCode}]";
            }

            byte[] body = await response.Content.ReadAsByteArrayAsync(cancel);

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
            if (json.ValueKind != JsonValueKind.Object) {
                return $"expected object from serialized JSON, got {json.ValueKind} instead";
            }

            BarMap map = new();
            map.ID = json.GetProperty("id").GetInt32();
            map.Name = json.GetRequiredString("scriptName");
            map.FileName = json.GetRequiredString("fileName");
            map.Description = json.GetString("description", "");
            map.TidalStrength = json.GetProperty("tidalStrength").GetDouble();
            map.MaxMetal = json.GetDouble("maxMetal", 0);
            map.ExtractorRadius = json.GetProperty("extractorRadius").GetDouble();
            map.MinimumWind = json.GetDouble("minWind", 0);
            map.MaximumWind = json.GetDouble("maxWind", 0);
            map.Height = json.GetProperty("height").GetDouble();
            map.Width = json.GetProperty("width").GetDouble();
            map.Author = json.GetString("author", "");

            return map;
        }

    }
}
