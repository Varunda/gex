using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarReplayParser {

        private readonly ILogger<BarReplayParser> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        public BarReplayParser(ILogger<BarReplayParser> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        public async Task<Result<BarMatch, string>> Parse(string filename, CancellationToken cancel = default) {
            string location = _Options.Value.ReplayLocation + Path.DirectorySeparatorChar + filename;
            string output = _Options.Value.TempWorkLocation + Path.DirectorySeparatorChar + $"output_{filename}.json";
            _Logger.LogInformation($"parsing replay file [location={location}]");

            Process nodeParser = new();
            nodeParser.StartInfo.FileName = "node";
            nodeParser.StartInfo.WorkingDirectory = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "node_parser";
            nodeParser.StartInfo.Arguments = $"./index.js \"{location}\" \"{output}\"";
            nodeParser.StartInfo.UseShellExecute = false;
            nodeParser.StartInfo.RedirectStandardOutput = true;
            Stopwatch timer = Stopwatch.StartNew();
            nodeParser.Start();

            string stdout = await nodeParser.StandardOutput.ReadToEndAsync(cancel);
            await nodeParser.WaitForExitAsync(cancel);

            _Logger.LogInformation($"parsed demo file [filename={filename}] [timer={timer.ElapsedMilliseconds}ms]");
            _Logger.LogDebug($"output: {stdout.TrimEnd()}");

            if (File.Exists(output) == false) {
                return $"missing output file \"{output}\"";
            }

            byte[] outputBytes = await File.ReadAllBytesAsync(output, cancel);
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(outputBytes);

            return ParseJson(json);
        }

        private Result<BarMatch, string> ParseJson(JsonElement json) {

            BarMatch match = new();

            JsonElement info = json.GetRequiredChild("info");
            JsonElement metaElem = info.GetRequiredChild("meta");

            // https://github.com/beyond-all-reason/spring/blob/fab2b206c49f4d3ad90a753e6be326ec88ae0ba6/rts/Game/GameSetup.h#L171
            // 	enum StartPosType {
            //      StartPos_Fixed            = 0,
            //      StartPos_Random           = 1,
            //      StartPos_ChooseInGame     = 2,
            //      StartPos_ChooseBeforeGame = 3,
            //      StartPos_Last             = 3  // last entry in enum (for user input check)
            //};
            int startingPosType = metaElem.GetProperty("startPosType").GetInt32();

            match.ID = metaElem.GetRequiredString("gameId");
            match.Engine = metaElem.GetRequiredString("engine");
            match.GameVersion = metaElem.GetRequiredString("game");
            match.Map = metaElem.GetRequiredString("map");
            match.StartTime = DateTime.Parse(metaElem.GetRequiredString("startTime"));
            match.DurationMs = metaElem.GetProperty("durationMs").GetInt64();

            match.HostSettings = info.GetRequiredChild("hostSettings");
            match.GameSettings = info.GetRequiredChild("gameSettings");
            match.MapSettings = info.GetRequiredChild("mapSettings");
            match.SpadsSettings = info.GetRequiredChild("spadsSettings");
            match.Restrictions = info.GetRequiredChild("restrictions");

            JsonElement allyTeams = info.GetRequiredChild("allyTeams");
            if (allyTeams.ValueKind != JsonValueKind.Array) {
                return $"expected allyTeams to be an array, got {allyTeams.ValueKind} instead";
            }

            foreach (JsonElement iter in allyTeams.EnumerateArray()) {
                BarMatchAllyTeam allyTeam = new();

                allyTeam.GameID = match.ID;
                allyTeam.AllyTeamID = iter.GetProperty("allyTeamId").GetInt32();
                allyTeam.PlayerCount = iter.GetProperty("playerCount").GetInt32();

                if (startingPosType == 1) { // random
                    allyTeam.StartBox = Rectangle.Zero;
                } else if (startingPosType == 2) { // teams ?
                    JsonElement startBox = iter.GetRequiredChild("startBox");
                    allyTeam.StartBox.Top = startBox.GetProperty("top").GetSingle();
                    allyTeam.StartBox.Bottom = startBox.GetProperty("bottom").GetSingle();
                    allyTeam.StartBox.Left = startBox.GetProperty("left").GetSingle();
                    allyTeam.StartBox.Right = startBox.GetProperty("right").GetSingle();
                } else {
                    throw new Exception($"unchecked value of startingPosType: {startingPosType}");
                }

                match.AllyTeams.Add(allyTeam);
            }

            JsonElement players = info.GetRequiredChild("players");
            if (players.ValueKind != JsonValueKind.Array) {
                return $"expected players to be an array, got {players.ValueKind} instead";
            }

            foreach (JsonElement iter in players.EnumerateArray()) {

                BarMatchPlayer player = new();

                player.GameID = match.ID;
                player.PlayerID = iter.GetProperty("playerId").GetInt64();
                player.UserID = iter.GetProperty("userId").GetInt64();
                player.Name = iter.GetString("name", "?");
                player.TeamID = iter.GetProperty("teamId").GetInt32();
                player.AllyTeamID = iter.GetProperty("allyTeamId").GetInt32();
                player.Faction = iter.GetRequiredString("faction");

                JsonElement startPosJson = iter.GetRequiredChild("startPos");
                Vector3 startingPosition = new() {
                    X = startPosJson.GetProperty("x").GetSingle(),
                    Y = startPosJson.GetProperty("y").GetSingle(),
                    Z = startPosJson.GetProperty("z").GetSingle(),
                };
                player.StartingPosition = startingPosition;

                JsonElement colorElem = iter.GetRequiredChild("rgbColor");
                player.Color = colorElem.GetProperty("r").GetByte() << 16
                    | colorElem.GetProperty("g").GetByte() << 8
                    | colorElem.GetProperty("b").GetByte() << 0;

                string skillStr = iter.GetRequiredString("skill");
                player.Skill = double.Parse(skillStr.Replace("[", "").Replace("]", ""));
                player.SkillUncertainty = iter.GetProperty("skillUncertainty").GetDouble();

                match.Players.Add(player);
            }

            return match;
        }

    }
}
