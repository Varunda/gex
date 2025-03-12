using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Event;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class ActionLogParser {

        private readonly ILogger<ActionLogParser> _Logger;

        public ActionLogParser(ILogger<ActionLogParser> logger) {
            _Logger = logger;
        }

        public async Task<Result<GameOutput, string>> Parse(string gameID, string file, CancellationToken cancel) {
            _Logger.LogDebug($"parsing action log [file={file}]");

            Stopwatch timer = Stopwatch.StartNew();
            string[] lines = await File.ReadAllLinesAsync(file, cancel);

            GameOutput output = new();
            output.GameID = gameID;

            bool errored = false;

            foreach (string line in lines) {
                cancel.ThrowIfCancellationRequested();

                JsonElement json = JsonSerializer.Deserialize<JsonElement>(line);

                string action = json.GetRequiredString("action");
                int frame = json.GetProperty("frame").GetInt32();

                try {
                    GameEvent ev;

                    if (action == GameActionType.INIT) {
                        continue;
                    }

                    else if (action == GameActionType.START) {
                        continue;
                    }

                    else if (action == GameActionType.WIND_UPDATE) {
                        GameEventWindUpdate e = JsonSerializer.Deserialize<GameEventWindUpdate>(json)!;
                        output.WindUpdates.Add(e);
                        ev = e;
                    }

                    else if (action == GameActionType.TEAM_DIED) {
                        GameEventTeamDied e = JsonSerializer.Deserialize<GameEventTeamDied>(json)!;
                        output.TeamDiedEvents.Add(e);
                        ev = e;
                    } 

                    else if (action == GameActionType.UNIT_DEF) {
                        GameEventUnitDef e = JsonSerializer.Deserialize<GameEventUnitDef>(json)!;
                        output.UnitDefinitions.Add(e);
                        ev = e;
                    }

                    else if (action == GameActionType.TEAM_STATS) {
                        GameEventTeamStats e = JsonSerializer.Deserialize<GameEventTeamStats>(json)!;
                        output.TeamStats.Add(e);
                        ev = e;
                    } 

                    else if (action == GameActionType.UNIT_CREATED) {
                        GameEventUnitCreated e = JsonSerializer.Deserialize<GameEventUnitCreated>(json)!;
                        output.UnitsCreated.Add(e);
                        ev = e;
                    }

                    else if (action == GameActionType.UNIT_KILLED) {
                        GameEventUnitKilled e = JsonSerializer.Deserialize<GameEventUnitKilled>(json)!;
                        output.UnitsKilled.Add(e);
                        ev = e;
                    }

                    else if (action == GameActionType.UNIT_GIVEN) {
                        ev = new GameEventUnitGiven();
                    }

                    else if (action == GameActionType.UNIT_TAKEN) {
                        ev = new GameEventUnitTaken();
                    }

                    else if (action == GameActionType.ARMY_VALUE_UPDATE) {
                        GameEventArmyValueUpdate e = JsonSerializer.Deserialize<GameEventArmyValueUpdate>(json)!;
                        output.ArmyValueUpdates.Add(e);
                        ev = e;
                    }

                    else if (action == GameActionType.END) {
                        continue;
                    }

                    else if (action == GameActionType.SHUTDOWN) {
                        ev = new GameEventShutdown();
                    }
                    
                    else {
                        _Logger.LogWarning($"unknown action [action={action}] [file={file}]");
                        continue;
                    }

                    ev.GameID = gameID;
                    ev.Action = action;
                    ev.Frame = frame;

                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to process line: {json}");
                    errored = true;
                }
            }

            if (errored == true) {
                return "got an error while parsing action log";
            }

            int unitDefHash = 0;
            foreach (GameEventUnitDef def in output.UnitDefinitions) {
                unitDefHash ^= def.GetDefinitionHash();
            }

            string hash = BitConverter.ToString(MD5.HashData([
                (byte)((unitDefHash >> 24) & 0xFF),
                (byte)((unitDefHash >> 16) & 0xFF),
                (byte)((unitDefHash >> 08) & 0xFF),
                (byte)((unitDefHash >> 00) & 0xFF)
            ])).Replace("-", "").ToLower();

            foreach (GameEventUnitDef def in output.UnitDefinitions) {
                def.Hash = hash;
            }

            _Logger.LogInformation($"parsed actions [gameID={gameID}] [timer={timer.ElapsedMilliseconds}ms] [file={file}]");

            return output;
        }

    }
}
