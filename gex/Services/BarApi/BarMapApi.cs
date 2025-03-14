﻿using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarMapApi {

        private readonly ILogger<BarMapApi> _Logger;

        private const string BASE_URL = "https://api.bar-rts.com/maps";

        private static readonly HttpClient _Http = new HttpClient();
        static BarMapApi() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

        public BarMapApi(ILogger<BarMapApi> logger) {
            _Logger = logger;
        }

        public async Task<Result<BarMap, string>> GetByName(string filename, CancellationToken cancel) {

            string url = BASE_URL + "/" + filename.Replace(" ", "_").ToLower();
            HttpResponseMessage response = await _Http.GetAsync(url);

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
            map.MaxMetal = json.GetProperty("maxMetal").GetDouble();
            map.ExtractorRadius = json.GetProperty("extractorRadius").GetDouble();
            map.MinimumWind = json.GetProperty("minWind").GetDouble();
            map.MaximumWind = json.GetProperty("maxWind").GetDouble();
            map.Height = json.GetProperty("height").GetDouble();
            map.Width = json.GetProperty("width").GetDouble();
            map.Author = json.GetString("author", "");

            return map;
        }


    }
}
