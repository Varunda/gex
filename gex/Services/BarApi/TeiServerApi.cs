using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Options;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class TeiServerApi {

        private readonly ILogger<TeiServerApi> _Logger;
        private readonly IOptions<BarApiOptions> _Options;
        private readonly BarApiMetric _BarApiMetric;

        private string _BaseUrl => $"{_Options.Value.TeiServer}/teiserver/api/public";

        private static readonly HttpClient _Http = new HttpClient();
        static TeiServerApi() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

        public TeiServerApi(ILogger<TeiServerApi> logger,
            IOptions<BarApiOptions> options, BarApiMetric barApiMetric) {

            _Logger = logger;
            _Options = options;
            _BarApiMetric = barApiMetric;
        }

        /// <summary>
        ///     get all seasons from TEI server
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns>a list of seasons</returns>
        /// <exception cref="System.Exception"></exception>
        public async Task<Result<List<BarSeason>, string>> GetSeasons(CancellationToken cancel) {
            _Logger.LogDebug($"getting seasons");

            Stopwatch timer = Stopwatch.StartNew();
            HttpResponseMessage response = await _Http.GetAsync($"{_BaseUrl}/leaderboard/", cancel);

            _BarApiMetric.RecordUse("tei-seasons");
            _BarApiMetric.RecordDuration("tei-seasons", timer.ElapsedMilliseconds / 1000d);

            if (response.IsSuccessStatusCode == false) {
                return $"failed to call TeiServer endpoint [status code={response.StatusCode}]";
            }

            byte[] body = await response.Content.ReadAsByteArrayAsync(cancel);

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
            if (json.ValueKind != JsonValueKind.Array) {
                return $"expected array from serialized JSON, got {json.ValueKind} instead";
            }

            List<BarSeason> seasons = [];

            foreach (JsonElement season in json.EnumerateArray()) {
                BarSeason szn = new();
                szn.Season = season.GetRequiredChild("season").GetInt32();

                JsonElement gameTypes = season.GetChild("game_types")
                    ?? throw new System.Exception($"missing game_types from {season}");

                foreach (JsonElement gt in gameTypes.EnumerateArray()) {
                    string gameType = gt.GetString()
                        ?? throw new System.Exception($"unexpected null in {gameTypes}");

                    szn.GameTypes.Add(gameType);
                }

                seasons.Add(szn);
            }

            return seasons;
        }

        /// <summary>
        ///     get the leaderboard for a specific season
        /// </summary>
        /// <param name="season">number of the season</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a list of the <see cref="BarLeaderboard"/>s in the current season
        /// </returns>
        /// <exception cref="System.Exception"></exception>
        public async Task<Result<List<BarLeaderboard>, string>> GetLeaderboard(int season, CancellationToken cancel) {
            _Logger.LogDebug($"getting leaderboard [season={season}]");

            Stopwatch timer = Stopwatch.StartNew();
            HttpResponseMessage response = await _Http.GetAsync($"{_BaseUrl}/leaderboard/{season}/", cancel);

            _BarApiMetric.RecordUse("tei-leaderboards");
            _BarApiMetric.RecordDuration("tei-leaderboards", timer.ElapsedMilliseconds / 1000d);

            if (response.IsSuccessStatusCode == false) {
                return $"failed to call TeiServer endpoint [status code={response.StatusCode}]";
            }

            byte[] body = await response.Content.ReadAsByteArrayAsync(cancel);

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
            if (json.ValueKind != JsonValueKind.Array) {
                return $"expected array from serialized JSON, got {json.ValueKind} instead";
            }

            List<BarLeaderboard> boards = [];

            foreach (JsonElement board in json.EnumerateArray()) {
                BarLeaderboard lb = new();
                lb.Gamemode = board.GetRequiredString("name");

                JsonElement players = board.GetChild("players")
                    ?? throw new System.Exception($"missing players from {board}");

                foreach (JsonElement player in players.EnumerateArray()) {
                    BarLeaderboardPlayer p = new();
                    p.UserID = player.GetRequiredChild("id").GetInt64();
                    p.Username = player.GetString("name", "<missing>");
                    p.Rating = (float)player.GetRequiredChild("rating").GetDouble();

                    lb.Players.Add(p);
                }

                boards.Add(lb);
            }

            return boards;
        }

    }
}
