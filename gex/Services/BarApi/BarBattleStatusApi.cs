using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Lobby;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarBattleStatusApi {

        private readonly ILogger<BarBattleStatusApi> _Logger;
        private readonly BarApiMetric _Metric;

        private const string BASE_URL = "https://api.bar-rts.com/battles";

        private static readonly Regex SKILL_PATTERN = new(@"\[(\d{1,3}(?:\.\d{0,2})?) \?+]");

        private static readonly HttpClient _Http = new HttpClient();
        static BarBattleStatusApi() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
        }

        public BarBattleStatusApi(ILogger<BarBattleStatusApi> logger,
            BarApiMetric metric) {

            _Logger = logger;
            _Metric = metric;
        }

        public async Task<Result<List<LobbyBattleStatus>, string>> GetAll(CancellationToken cancel) {
            _Logger.LogDebug($"getting battle status from API [url={BASE_URL}]");

            Stopwatch timer = Stopwatch.StartNew();
            Result<JsonElement, string> res = await _Http.GetJsonAsync(BASE_URL, cancel);
            _Metric.RecordDuration("battles", timer.ElapsedMilliseconds / 1000d);
            _Metric.RecordUse("battles");

            if (res.IsOk == false) {
                return res.Error;
            }

            JsonElement json = res.Value;
            if (json.ValueKind != JsonValueKind.Array) {
                return $"expected JSON response to be an array, is a {json.ValueKind} instead";
            }

            List<LobbyBattleStatus> statuses = [];

            foreach (JsonElement iter in json.EnumerateArray()) {
                LobbyBattleStatus status = new();
                status.BattleID = iter.GetRequiredChild("battleId").GetInt32();
                status.Timestamp = DateTime.UtcNow;

                JsonElement players = iter.GetRequiredChild("players");
                if (players.ValueKind != JsonValueKind.Array) {
                    throw new Exception($"expected 'players' to be an array, is a {players.ValueKind} instead");
                }

                foreach (JsonElement player in players.EnumerateArray()) {
                    long userId = player.GetRequiredChild("userId").GetInt64();
                    string username = player.GetRequiredChild("username").GetString() ?? "<missing username>";

                    double skill = 0d;
                    string skillValue = player.GetChild("skill")?.GetString() ?? "[16.67 ???]";
                    if (skillValue != "") {
                        Match skillMatch = SKILL_PATTERN.Match(skillValue);
                        if (skillMatch.Success == true) {
                            if (skillMatch.Groups.Count >= 2) {
                                string skillStr = skillMatch.Groups[1].Value; // 0 is input
                                if (double.TryParse(skillStr, out skill) == false) {
                                    _Logger.LogWarning($"failed to parse matched skill capture into a valid double [skillStr={skillStr}] [battleID={status.BattleID}]");
                                }
                            } else {
                                _Logger.LogWarning($"missing capture of skill fo lobby client [skillValue={skillValue}] [battleID={status.BattleID}]");
                            }
                        } else {
                            _Logger.LogWarning($"failed to get skill of lobby client [skillValue={skillValue}] [client={player}] [battleID={status.BattleID}] [regex={SKILL_PATTERN}]");
                        }
                    }

                    if (player.GetChild("teamId") == null) {
                        LobbyBattleStatusSpectator spec = new();
                        spec.UserID = userId;
                        spec.Username = username;
                        spec.Skill = skill;

                        status.Spectators.Add(spec);
                    } else {
                        int playerId = player.GetRequiredChild("teamId").GetInt32();

                        LobbyBattleStatusClient client = new();
                        client.UserID = userId;
                        client.Username = username;
                        client.PlayerID = playerId;
                        client.Skill = skill;

                        status.Clients.Add(client);
                    }
                }

                statuses.Add(status);
            }

            return statuses;
        }


    }
}
