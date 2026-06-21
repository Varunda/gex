using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Db;
using gex.Models.Map;
using gex.Services.Db;
using gex.Services.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    public class StartSpotDataParser {

        private readonly ILogger<StartSpotDataParser> _Logger;

        public StartSpotDataParser(ILogger<StartSpotDataParser> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     parse a <see cref="StartSpotData"/> from a base64 zlib compressed string
        /// </summary>
        /// <param name="mapFilename"></param>
        /// <param name="b64"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<StartSpotData, string>> ParseB64(string mapFilename, string b64, CancellationToken cancel) {
            byte[] compressed = Base64Url.DecodeFromChars(b64);

            Result<byte[], string> decomp = await SafeZLib.Decompress(compressed, 1024 * 1024 * 4, cancel);
            if (decomp.IsOk == false) {
                return decomp.Error;
            }

            string jsonStr = Encoding.UTF8.GetString(decomp.Value);
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(jsonStr);

            return Parse(mapFilename, json);
        }

        /// <summary>
        ///     parse a <see cref="StartSpotData"/> from a JsonElement
        /// </summary>
        /// <param name="mapFilename"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public Result<StartSpotData, string> Parse(string mapFilename, JsonElement json) {
            StartSpotData ret = new();
            ret.MapFilename = mapFilename;
            ret.Timestamp = DateTime.UtcNow;
            ret.MinTimestamp = DateTime.UtcNow;
            ret.Raw = json;

            JsonElement positions = json.GetRequiredChild("positions");
            foreach (JsonProperty obj in positions.EnumerateObject()) {
                StartSpotPosition position = new();
                position.MapFilename = mapFilename;
                position.Name = obj.Name;
                position.X = obj.Value.GetRequiredChild("x").GetSingle();
                position.Y = obj.Value.GetRequiredChild("y").GetSingle();

                ret.Positions.Add(position);
            }
            ret.Positions = ret.Positions.OrderBy(iter => iter.Name).ToList();

            JsonElement configs = json.GetRequiredChild("team");
            foreach (JsonElement iter in configs.EnumerateArray()) {
                StartSpotConfiguration config = new();
                config.MapFilename = mapFilename;
                config.PlayersPerTeam = iter.GetRequiredChild("playersPerTeam").GetInt32();
                config.TeamCount = iter.GetRequiredChild("teamCount").GetInt32();

                int index = 0;
                JsonElement sides = iter.GetRequiredChild("sides");
                foreach (JsonElement side in sides.EnumerateArray()) {
                    StartSpotSide startSide = new();
                    startSide.PlayersPerTeam = config.PlayersPerTeam;
                    startSide.TeamCount = config.TeamCount;
                    startSide.MapFilename = mapFilename;
                    startSide.Index = index++;

                    JsonElement starts = side.GetRequiredChild("starts");
                    foreach (JsonElement start in starts.EnumerateArray()) {
                        StartSpotSideStart startStart = new();
                        startStart.MapFilename = mapFilename;
                        startStart.SideIndex = startSide.Index;
                        startStart.SpawnPoint = start.GetRequiredString("spawnPoint");
                        startStart.BaseCenter = start.NullableString("baseCenter");

                        startStart.Role = start.NullableString("role") ?? "<missing>";
                        startStart.BaseRole = startStart.Role;

                        startSide.Starts.Add(startStart);
                    }
                    startSide.Starts = startSide.Starts.OrderBy(iter => iter.SpawnPoint).ToList();

                    config.Sides.Add(startSide);
                }
                config.Sides = config.Sides.OrderBy(iter => iter.Index).ToList();

                ret.Configurations.Add(config);
            }

            return ret;
        }

    }
}
