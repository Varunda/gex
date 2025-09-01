using gex.Code;
using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class ActionLogParser {

        private readonly ILogger<ActionLogParser> _Logger;
        private readonly JsonSerializerOptions _JsonOptions;

        public ActionLogParser(ILogger<ActionLogParser> logger) {
            _Logger = logger;

            _JsonOptions = new JsonSerializerOptions() {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver() {
                    Modifiers = {
                        JsonExtensions.UseActionLogNames<JsonActionLogPropertyNameAttribute>()
                    }
                }
            };
        }

        public async Task<Result<GameOutput, string>> Parse(string gameID, string file, CancellationToken cancel) {
            _Logger.LogDebug($"parsing action log [file={file}]");

            Stopwatch timer = Stopwatch.StartNew();
            string[] lines = await File.ReadAllLinesAsync(file, cancel);

            GameOutput output = new();
            output.GameID = gameID;

            bool errored = false;

            Dictionary<string, int> unknownCount = [];

            int lineNumber = 0;
            foreach (string line in lines) {
                cancel.ThrowIfCancellationRequested();
                ++lineNumber;

                JsonElement json;
                try {
                    json = JsonSerializer.Deserialize<JsonElement>(line);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to deserialize line [gameID={gameID}] [num={lineNumber}] [line={line}]");
                    errored = true;
                    continue;
                }

                string action = json.GetRequiredString("action");
                int frame = json.GetProperty("frame").GetInt32();

                try {
                    GameEvent ev;

                    if (action == GameActionType.INIT) {
                        _Logger.LogDebug($"action log init [gameID={gameID}] [version={json}]");
                        continue;
                    } else if (action == GameActionType.START) {
                        continue;
                    } else if (action == GameActionType.WIND_UPDATE) {
                        GameEventWindUpdate e = Serialize<GameEventWindUpdate>(json)!;
                        output.WindUpdates.Add(e);
                        ev = e;
                    } else if (action == GameActionType.TEAM_DIED) {
                        GameEventTeamDied e = Serialize<GameEventTeamDied>(json)!;
                        e.Frame = frame;
                        if (output.TeamDiedEvents.FirstOrDefault(iter => iter.Frame == e.Frame && iter.TeamID == e.TeamID) != null) {
                            _Logger.LogWarning($"duplicate {nameof(GameEventTeamDied)} found [gameID={gameID}] [frame={e.Frame}] [teamID={e.TeamID}]");
                        } else {
                            output.TeamDiedEvents.Add(e);
                        }
                        ev = e;
                    } else if (action == GameActionType.UNIT_DEF) {
                        GameEventUnitDef e = Serialize<GameEventUnitDef>(json)!;
                        output.UnitDefinitions.Add(e);
                        ev = e;
                    } else if (action == GameActionType.TEAM_STATS) {
                        GameEventTeamStats e = Serialize<GameEventTeamStats>(json)!;
                        if (false && output.TeamStats.FirstOrDefault(iter => iter.Frame == e.Frame && iter.TeamID == e.TeamID) != null) {
                            _Logger.LogWarning($"duplicate {nameof(GameEventTeamStats)} found [gameID={gameID}] [frame={e.Frame}] [teamID={e.TeamID}]");
                        } else {
                            output.TeamStats.Add(e);
                        }

                        ev = e;
                    } else if (action == GameActionType.UNIT_CREATED) {
                        GameEventUnitCreated e = Serialize<GameEventUnitCreated>(json)!;
                        output.UnitsCreated.Add(e);
                        ev = e;
                    } else if (action == GameActionType.UNIT_KILLED) {
                        GameEventUnitKilled e = Serialize<GameEventUnitKilled>(json)!;
                        output.UnitsKilled.Add(e);
                        ev = e;
                    } else if (action == GameActionType.UNIT_GIVEN) {
                        // somehow there are duplicate events here, from the game itself
                        GameEventUnitGiven e = Serialize<GameEventUnitGiven>(json);
                        e.Frame = frame;
                        if (output.UnitsGiven.FirstOrDefault(iter => iter.Frame == e.Frame && iter.UnitID == e.UnitID) != null) {
                            //_Logger.LogWarning($"duplicate UnitGiven event found [gameID={gameID}] [frame={e.Frame}] [unitID={e.UnitID}]");
                        } else {
                            output.UnitsGiven.Add(e);
                        }

                        ev = e;
                    } else if (action == GameActionType.UNIT_TAKEN) {
                        GameEventUnitTaken e = Serialize<GameEventUnitTaken>(json);
                        e.Frame = frame;
                        if (output.UnitsTaken.FirstOrDefault(iter => iter.Frame == e.Frame && iter.UnitID == e.UnitID) != null) {
                            //_Logger.LogWarning($"duplicate UnitTaken event found [gameID={gameID}] [frame={e.Frame}] [unitID={e.UnitID}]");
                        } else {
                            output.UnitsTaken.Add(e);
                        }
                        ev = e;
                    } else if (action == GameActionType.FACTORY_UNIT_CREATE) {
                        GameEventFactoryUnitCreated e = Serialize<GameEventFactoryUnitCreated>(json)!;
                        output.FactoryUnitCreated.Add(e);
                        ev = e;
                    } else if (action == GameActionType.EXTRA_STATS) {
                        GameEventExtraStatUpdate e = Serialize<GameEventExtraStatUpdate>(json)!;
                        output.ExtraStats.Add(e);
                        ev = e;
                    } else if (action == GameActionType.COMMANDER_POSITION_UPDATE) {
                        GameEventCommanderPositionUpdate e = Serialize<GameEventCommanderPositionUpdate>(json);
                        output.CommanderPositionUpdates.Add(e);
                        ev = e;
                    } else if (action == GameActionType.TRANSPORT_LOADED) {
                        GameEventUnitTransportLoaded e = Serialize<GameEventUnitTransportLoaded>(json);
                        output.TransportLoaded.Add(e);
                        ev = e;
                    } else if (action == GameActionType.TRANSPORT_UNLOADED) {
                        GameEventUnitTransportUnloaded e = Serialize<GameEventUnitTransportUnloaded>(json);
                        output.TransportUnloaded.Add(e);
                        ev = e;
                    } else if (action == GameActionType.UNIT_RESOURCES) {
                        GameEventUnitResources e = Serialize<GameEventUnitResources>(json);
                        output.UnitResources.Add(e);
                        ev = e;
                    } else if (action == GameActionType.UNIT_DAMAGE) {
                        GameEventUnitDamage e = Serialize<GameEventUnitDamage>(json);
                        output.UnitDamage.Add(e);
                        ev = e;
                    } else if (action == GameActionType.UNIT_POSITION) {
                        GameEventUnitPosition e = Serialize<GameEventUnitPosition>(json);
                        output.UnitPosition.Add(e);
                        ev = e;
                    } else if (action == GameActionType.END) {
                        continue;
                    } else if (action == GameActionType.SHUTDOWN) {
                        ev = new GameEventShutdown();
                    } else {
                        if (unknownCount.ContainsKey(action) == false) {
                            _Logger.LogWarning($"unknown action [action={action}] [json={json}] [file={file}]");
                            unknownCount.Add(action, 0);
                        }
                        unknownCount[action] = unknownCount[action] + 1;
                        continue;
                    }

                    ev.GameID = gameID;
                    ev.Action = action;
                    ev.Frame = frame;

                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to process line [gameID={gameID}] [lin num={lineNumber}]: {json}");
                    errored = true;
                }
            }

            foreach (KeyValuePair<string, int> entry in unknownCount) {
                _Logger.LogWarning($"unknown action [action={entry.Key}] [count={entry.Value}] [file={file}]");
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

        private T Serialize<T>(JsonElement json) {
            return JsonSerializer.Deserialize<T>(json, _JsonOptions)!;
        }

    }
}
