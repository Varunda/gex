﻿using gex.Code;
using gex.Code.Constants;
using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Db;
using gex.Models.Demofile;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    /// <summary>
    ///     service that takes in the bytes of a demofile and parses it into a <see cref="BarMatch"/>.
    ///     much of this code was based on https://github.com/beyond-all-reason/demo-parser
    /// </summary>
    public class BarDemofileParser {

        private readonly ILogger<BarDemofileParser> _Logger;

        public BarDemofileParser(ILogger<BarDemofileParser> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     parse the bytes of a demo file returning a parsed <see cref="BarMatch"/>
        /// </summary>
        /// <param name="filename">name of the demofile</param>
        /// <param name="demofile">compressed bytes of the demofile</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     a <see cref="Result{T, E}"/> that contains a <see cref="BarMatch"/> if <paramref name="demofile"/>
        ///     was successfully parsed, or the error value will contain a string indicating what error took place
        ///     during parsing 
        /// </returns>
        public async Task<Result<BarMatch, string>> Parse(string filename, byte[] demofile, CancellationToken cancel) {
            Stopwatch timer = Stopwatch.StartNew();
            using MemoryStream stream = new(demofile);
            using GZipStream zlib = new(stream, CompressionMode.Decompress);
            using MemoryStream output = new();

            // this is copied from CopyToAsync, but includes a failsafe where if the demofile 
            // gets too large when unzipping, processing exits (anti-zip-bomb)
            //await zlib.CopyToAsync(output, cancel);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            try {
                int totalRead = 0;
                int bytesRead;
                while ((bytesRead = await zlib.ReadAsync(new Memory<byte>(buffer), cancel).ConfigureAwait(false)) != 0) {
                    totalRead += bytesRead;
                    await output.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancel).ConfigureAwait(false);

                    // if the demofile uncompressed is over 256MB, something is very wrong :tm:
                    // largest demo file i have is 20MB compressed, 56MB uncompressed 
                    if (totalRead > 1024 * 1024 * 256) {
                        _Logger.LogError($"uncompressed demofile reached unsafe size, exiting [filename={filename}]");
                        return $"demofile uncompression reached unsafe size";
                    }
                }
            } finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            byte[] data = output.ToArray();
            _Logger.LogDebug($"decompressed demofile [duration={timer.ElapsedMilliseconds}ms] [input size={demofile.Length}] [output size={data.Length}]");

            return ReadBytes(filename, data);
        }

        private Result<BarMatch, string> ReadBytes(string filename, byte[] data) {
            ByteArrayReader reader = new(data);

            Demofile demofile = new();

            BarMatch match = new();
            match.FileName = filename;

            DemofileHeader header = new();
            header.Magic = reader.ReadAsciiStringNullTerminated(16);
            if (header.Magic != "spring demofile") {
                return $"expected 'spring demofile' from magic, got '{header.Magic}' instead";
            }

            header.HeaderVersion = reader.ReadInt32LE();
            header.HeaderSize = reader.ReadInt32LE();
            header.GameVersion = reader.ReadAsciiString(256);
            header.GameVersion = header.GameVersion[..header.GameVersion.IndexOf('\0')]; // remove all the null terms at the end
            header.GameID = BitConverter.ToString(reader.Read(16).ToArray()).Replace("-", "").ToLowerInvariant();
            header.StartTime = reader.ReadInt64LE();
            header.ScriptSize = reader.ReadInt32LE();
            header.DemoStreamSize = reader.ReadInt32LE();
            header.GameTime = reader.ReadInt32LE();
            header.WallClockTime = reader.ReadInt32LE();
            header.PlayerCount = reader.ReadInt32LE();
            header.PlayerStatSize = reader.ReadInt32LE();
            header.PlayerStatElemSize = reader.ReadInt32LE();
            header.TeamCount = reader.ReadInt32LE();
            header.TeamStatSize = reader.ReadInt32LE();
            header.TeamStatElemSize = reader.ReadInt32LE();
            header.TeamStatPeriod = reader.ReadInt32LE();
            header.WinningAllyTeamsSize = reader.ReadInt32LE();
            demofile.Header = header;

            match.ID = header.GameID;

            if (reader.Index != 0x0160) {
                return $"expected reader index to be at {0x0160}, was at {reader.Index} instead";
            }

            string modConfig = "{" + reader.ReadAsciiString(header.ScriptSize) + "}";
            demofile.ModConfig = modConfig;
            // yummy regex, this turns the mod config into json
            modConfig = new Regex(@"([^=\w\]\[])(\[(.*?)\])").Replace(modConfig, "$1\"$3\":");
            modConfig = new Regex("^(\\w*)\\=(.*?);", RegexOptions.Multiline).Replace(modConfig, "\"$1\": \"$2\",");
            modConfig = new Regex("\\r|\\n", RegexOptions.Multiline).Replace(modConfig, "");
            modConfig = new Regex("\\\",}", RegexOptions.Multiline).Replace(modConfig, "\"}");
            modConfig = new Regex("}\"", RegexOptions.Multiline).Replace(modConfig, "},\"");

            JsonElement modJson = JsonSerializer.Deserialize<JsonElement>(modConfig);
            JsonElement game = modJson.GetRequiredChild("game");

            Dictionary<int, BarMatchPlayer> players = []; // <teamID, player>

            // i hate the c sharp json api actually, can't do anything with it
            Dictionary<string, string> hostSettings = new Dictionary<string, string>();
            foreach (JsonProperty iter in game.EnumerateObject()) {
                if (iter.Value.ValueKind == JsonValueKind.String) {
                    // anything that's just chilling in the mod config not under a section is part of the host settings
                    hostSettings.Add(iter.Name, iter.Value.GetString()!);
                } else if (iter.Value.ValueKind == JsonValueKind.Object) {

                    // team parsing
                    if (iter.Name.StartsWith("team")) {
                        int teamID = int.Parse(iter.Name.Split("team")[1]);
                        BarMatchPlayer player = players.GetValueOrDefault(teamID) ?? new BarMatchPlayer();
                        player.TeamID = teamID;
                        player.GameID = match.ID;
                        player.AllyTeamID = iter.Value.GetRequiredInt32("allyteam");
                        player.Handicap = iter.Value.GetDecimal("handicap", 0m);
                        player.Faction = iter.Value.GetRequiredString("side");

                        players[player.TeamID] = player;
                    }

                    // ally team parsing
                    else if (iter.Name.StartsWith("ally")) {
                        int allyTeamID = int.Parse(iter.Name.Split("allyteam")[1]);
                        BarMatchAllyTeam allyTeam = new();
                        allyTeam.AllyTeamID = allyTeamID;
                        allyTeam.GameID = match.ID;
                        allyTeam.StartBox.Top = iter.Value.GetFloat("startrecttop", 0f);
                        allyTeam.StartBox.Bottom = iter.Value.GetFloat("startrectbottom", 0f);
                        allyTeam.StartBox.Left = iter.Value.GetFloat("startrectleft", 0f);
                        allyTeam.StartBox.Right = iter.Value.GetFloat("startrectright", 0f);

                        BarMatchAllyTeam? existing = match.AllyTeams.FirstOrDefault(iter => iter.AllyTeamID == allyTeamID);
                        if (existing != null) {
                            _Logger.LogWarning($"multiple ally teams with the same id [id={allyTeamID}]");
                        } else {
                            match.AllyTeams.Add(allyTeam);
                        }
                    }

                    // player parsing, also spectator
                    else if (iter.Name.StartsWith("player")) {

                        if (iter.Value.GetRequiredString("spectator") == "1") {
                            if (iter.Value.GetChild("accountid") == null) {
                                _Logger.LogWarning($"missing accountid from {iter.Value}");
                            } else {
                                BarMatchSpectator spec = new();
                                spec.GameID = match.ID;
                                spec.PlayerID = int.Parse(iter.Name.Split("player")[1]);
                                spec.UserID = iter.Value.GetRequiredInt64("accountid");
                                spec.Name = iter.Value.GetRequiredString("name");

                                match.Spectators.Add(spec);
                            }
                        } else {
                            int teamID = iter.Value.GetRequiredInt32("team");
                            BarMatchPlayer player = players.GetValueOrDefault(teamID) ?? new BarMatchPlayer();
                            player.TeamID = teamID;
                            player.GameID = match.ID;
                            player.PlayerID = int.Parse(iter.Name.Split("player")[1]);
                            player.UserID = iter.Value.GetRequiredInt64("accountid");
                            player.Name = iter.Value.GetRequiredString("name");
                            player.SkillUncertainty = double.Parse(iter.Value.GetProperty("skilluncertainty").GetString()!);
                            player.Skill = double.Parse(iter.Value.GetProperty("skill").GetString()!.Replace("[", "").Replace("]", "")); // remove the [] around the skill

                            players[player.TeamID] = player;
                        }
                    } else if (iter.Name.StartsWith("ai")) {
                        // [ai0]
                        int aiID = int.Parse(iter.Name.Split("ai")[1]);
                        int teamID = iter.Value.GetRequiredInt32("team");

                        BarMatchAiPlayer ai = new();
                        ai.AiID = aiID;
                        ai.TeamID = teamID;
                        ai.Name = iter.Value.GetRequiredString("name");
                        match.AiPlayers.Add(ai);
                    }

                }
            }

            match.HostSettings = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(hostSettings));

            match.GameSettings = game.GetRequiredChild("modoptions");
            match.MapSettings = game.GetRequiredChild("mapoptions");
            match.SpadsSettings = game.GetRequiredChild("hostoptions");
            match.Restrictions = game.GetRequiredChild("restrict");

            match.Engine = header.GameVersion;
            match.GameVersion = hostSettings.GetValueOrDefault("gametype") ?? "";
            match.Map = hostSettings.GetValueOrDefault("mapname") ?? "";
            match.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(header.StartTime * 1000).ToUniversalTime().DateTime;
            match.DurationMs = header.WallClockTime * 1000;

            if (header.PacketOffset != reader.Index) {
                return $"expected reader to be {header.PacketOffset} (for reading packets), was at {reader.Index} instead";
            }

            Dictionary<int, string> unitDefDict = [];

            byte[] winningAllyTeams = [];

            int packetCount = 0;
            int frameCount = 0;
            int maxFrame = 0;
            while (reader.Index < header.StatOffset) {
                DemofilePacket packet = new();
                packet.GameTime = reader.ReadFloat32LE();
                packet.Length = reader.ReadUInt32LE();
                packet.PacketType = reader.ReadByte();

                Span<byte> packetData = reader.Read(packet.Length - 1);
                packet.Data = packetData.ToArray();
                ++packetCount;

                if (packet.PacketType == BarPacketType.CHAT) {
                    ByteArrayReader packetReader = new(packet.Data);

                    BarMatchChatMessage msg = new();
                    msg.Size = packetReader.ReadByte();
                    msg.FromId = packetReader.ReadByte();
                    msg.ToId = packetReader.ReadByte(); // 127 = allies, 126 = spec, 125 = global
                    msg.Message = Encoding.ASCII.GetString(packetReader.ReadUntilNull());
                    msg.GameID = match.ID;
                    msg.GameTimestamp = packet.GameTime;

                    match.ChatMessages.Add(msg);
                } else if (packet.PacketType == BarPacketType.GAME_ID) {
                    ByteArrayReader packetReader = new(packet.Data);
                    string packetGameID = BitConverter.ToString(packetReader.Read(16).ToArray()).Replace("-", "").ToLowerInvariant();

                    if (packetGameID != header.GameID) {
                        return $"inconsistent gameID found, refusing to process further";
                    }

                } else if (packet.PacketType == BarPacketType.START_POS) {
                    ByteArrayReader packetReader = new(packet.Data);
                    byte playerID = packetReader.ReadByte();
                    byte teamID = packetReader.ReadByte();
                    byte readyState = packetReader.ReadByte();
                    float x = packetReader.ReadFloat32LE();
                    float y = packetReader.ReadFloat32LE();
                    float z = packetReader.ReadFloat32LE();

                    BarMatchPlayer? player = players.GetValueOrDefault(teamID);
                    if (player == null) {
                        _Logger.LogWarning($"cannot set start position, team does not exist [teamID={teamID}] [gameID={header.GameID}]");
                    } else {
                        player.StartingPosition = new System.Numerics.Vector3() {
                            X = x,
                            Y = y,
                            Z = z
                        };
                    }

                } else if (packet.PacketType == BarPacketType.LUA_MSG) {
                    ByteArrayReader packetReader = new(packet.Data);
                    short size = packetReader.ReadInt16LE();
                    byte playerNum = packetReader.ReadByte();
                    short script = packetReader.ReadInt16LE();
                    byte mode = packetReader.ReadByte();

                    byte[] bytes = packetReader.ReadAll();
                    string msg = Encoding.ASCII.GetString(bytes);

                    if (msg.StartsWith("AutoColors")) {
                        JsonElement colors = JsonSerializer.Deserialize<JsonElement>(msg.Substring(10));

                        foreach (JsonElement iter in colors.EnumerateArray()) {
                            int teamID = iter.GetProperty("teamID").GetInt32();

                            // TODO: why can these values go can over 255 and below 0 
                            byte r = (byte)Math.Min(255, Math.Max(0, iter.GetProperty("r").GetInt32()));
                            byte g = (byte)Math.Min(255, Math.Max(0, iter.GetProperty("g").GetInt32()));
                            byte b = (byte)Math.Min(255, Math.Max(0, iter.GetProperty("b").GetInt32()));

                            BarMatchPlayer? player = players.GetValueOrDefault(teamID);
                            if (player != null) {
                                player.Color = (r << 16) | (g << 8) | b;
                            }
                        }
                    } else if (msg.StartsWith("changeStartUnit")) {
                        int unitDefID = int.Parse(msg.Substring("changeStartUnit".Length));
                        _Logger.LogTrace($"player changing factions [playerNum={playerNum}] [unitDefID={unitDefID}] [gameID={header.GameID}]");

                        string? defName = unitDefDict.GetValueOrDefault(unitDefID);
                        if (defName == null) {
                            _Logger.LogWarning($"missing unit definition in changeStartUnit! [gameID={header.GameID}] [def ID={unitDefID}] [playerID={playerNum}]");
                        } else {
                            BarMatchPlayer? player = players.GetValueOrDefault(playerNum);
                            if (player != null) {
                                if (defName == "armcom") {
                                    player.Faction = "Armada";
                                } else if (defName == "corcom") {
                                    player.Faction = "Cortex";
                                } else if (defName == "legcom") {
                                    player.Faction = "Legion";
                                } else if (defName == "dummycom") {
                                    player.Faction = "Random";
                                } else {
                                    _Logger.LogWarning($"unchecked defName for changeStartUnit [id={header.GameID}] [def name={defName}]");
                                }
                                _Logger.LogTrace($"player changed factions [playerNum={playerNum}] [faction={player.Faction}] [unitDefID={unitDefID}] [gameID={header.GameID}]");
                            }
                        }

                    } else if (msg.StartsWith("unitdefs:")) {
                        Span<byte> input = bytes.AsSpan("unitdefs:".Length);
                        using MemoryStream stream = new(input.ToArray());
                        using ZLibStream zlib = new(stream, CompressionMode.Decompress);
                        using MemoryStream output = new();
                        zlib.CopyTo(output);
                        byte[] unitDefs = output.ToArray();

                        JsonElement json = JsonSerializer.Deserialize<JsonElement>(unitDefs);

                        int index = 1; // i'm gonna write a mean comment about why this index starts at 1 instead of 0
                        foreach (JsonElement iter in json.EnumerateArray()) {
                            string defName = iter.GetString()!;
                            if (unitDefDict.ContainsKey(index)) {
                                if (unitDefDict[index] != defName) {
                                    _Logger.LogWarning($"inconsistent def names! [gameID={header.GameID}] [index={index}] [current={unitDefDict[index]}] [new={defName}]");
                                }
                            } else {
                                unitDefDict.Add(index, iter.GetString()!);
                            }
                            index += 1;
                        }
                    }
                } else if (packet.PacketType == BarPacketType.KEYFRAME) {
                    ByteArrayReader packetReader = new(packet.Data);
                    int frame = packetReader.ReadInt32LE();

                    maxFrame = Math.Max(frameCount, frame);
                } else if (packet.PacketType == BarPacketType.NEW_FRAME) {
                    // it seems like this isn't accurate
                    ++frameCount;
                } else if (packet.PacketType == BarPacketType.GAME_OVER) {
                    ByteArrayReader packetReader = new(packet.Data);
                    byte size = packetReader.ReadByte();
                    byte playerNum = packetReader.ReadByte();
                    winningAllyTeams = packetReader.ReadAll();
                } else if (packet.PacketType == BarPacketType.TEAM_MSG) {
                    ByteArrayReader packetReader = new(packet.Data);
                    byte playerNum = packetReader.ReadByte();
                    byte action = packetReader.ReadByte();
                    byte param1 = packetReader.ReadByte();

                    if (action == 2) { // 2 = resigned
                        BarMatchTeamDeath death = new();
                        death.GameID = header.GameID;
                        death.TeamID = playerNum;
                        death.Reason = action;
                        death.GameTime = packet.GameTime;
                        match.TeamDeaths.Add(death);
                    } else if (action == 4) { // 4 = TEAM_DIED, param1 = team that died
                        BarMatchTeamDeath death = new();
                        death.GameID = header.GameID;
                        death.TeamID = param1;
                        death.Reason = action;
                        death.GameTime = packet.GameTime;
                        match.TeamDeaths.Add(death);
                    }
                } else if (packet.PacketType == BarPacketType.QUIT) {
                    _Logger.LogDebug($"found packet type 3, breaking [index={reader.Index}] [packet count={packetCount}]");
                    break;
                }
            }

            if (header.StatOffset != reader.Index) {
                return $"expected reader to be {header.StatOffset} (for reading stats), was at {reader.Index} instead";
            }

            _Logger.LogDebug($"packets parsed [gameID={match.ID}] [packet count={packetCount}] [frame count={frameCount}] [max frame={maxFrame}]");
            match.DurationFrameCount = maxFrame;

            // player stat parsing
            DemofileStatistics playerStats = new();
            for (int i = 0; i < header.WinningAllyTeamsSize; ++i) {
                playerStats.WinningAllyTeamIDs.Add(reader.ReadByte());
            }

            for (int i = 0; i < header.PlayerCount; ++i) {
                DemofilePlayerStats iter = new();
                iter.PlayerID = i;
                iter.CommandCount = reader.ReadInt32LE();
                iter.UnitCommands = reader.ReadInt32LE();
                iter.MousePixels = reader.ReadInt32LE();
                iter.MouseClicks = reader.ReadInt32LE();
                iter.KeyPresses = reader.ReadInt32LE();

                playerStats.PlayerStats.Add(iter);
            }

            demofile.Statistics = playerStats;

            // team stat parsing
            List<DemofileTeamStats> teamStats = [];
            for (int i = 0; i < header.TeamCount; ++i) {
                teamStats.Add(new DemofileTeamStats() {
                    TeamID = i,
                    StatCount = reader.ReadInt32LE()
                });
            }

            for (int i = 0; i < header.TeamCount; ++i) {
                DemofileTeamStats iter = teamStats[i];

                for (int j = 0; j < iter.StatCount; ++j) {
                    DemofileTeamFrameStats frame = new();
                    frame.TeamID = iter.TeamID;
                    frame.Frame = reader.ReadInt32LE();
                    frame.MetalUsed = reader.ReadFloat32LE();
                    frame.EnergyUsed = reader.ReadFloat32LE();
                    frame.MetalProduced = reader.ReadFloat32LE();
                    frame.EnergyProduced = reader.ReadFloat32LE();
                    frame.MetalExcess = reader.ReadFloat32LE();
                    frame.EnergyExcess = reader.ReadFloat32LE();
                    frame.MetalReceived = reader.ReadFloat32LE();
                    frame.EnergyReceived = reader.ReadFloat32LE();
                    frame.MetalSend = reader.ReadFloat32LE();
                    frame.EnergySend = reader.ReadFloat32LE();
                    frame.DamageDealt = reader.ReadFloat32LE();
                    frame.DamageReceived = reader.ReadFloat32LE();
                    frame.UnitsProduced = reader.ReadInt32LE();
                    frame.UnitsDied = reader.ReadInt32LE();
                    frame.UnitsReceived = reader.ReadInt32LE();
                    frame.UnitsSent = reader.ReadInt32LE();
                    frame.UnitsCaptured = reader.ReadInt32LE();
                    frame.UnitsOutCaptured = reader.ReadInt32LE();
                    frame.UnitsKilled = reader.ReadInt32LE();

                    iter.Entries.Add(frame);
                }
            }

            demofile.TeamStatistics = teamStats;

            match.Players = players.Values.ToList();
            foreach (BarMatchAllyTeam allyTeam in match.AllyTeams) {
                if (winningAllyTeams.Contains((byte)allyTeam.AllyTeamID)) {
                    allyTeam.Won = true;
                }
                allyTeam.PlayerCount = match.Players.Count(iter => iter.AllyTeamID == allyTeam.AllyTeamID);
                match.PlayerCount += allyTeam.PlayerCount;
            }

            int largestAllyTeam = match.AllyTeams.Select(iter => iter.PlayerCount).Max();
            int allyTeamCount = match.AllyTeams.Count;

            if (allyTeamCount == 2 && largestAllyTeam == 1) {
                match.Gamemode = BarGamemode.DUEL;
            } else if (allyTeamCount == 2 && largestAllyTeam <= 5) {
                match.Gamemode = BarGamemode.SMALL_TEAM;
            } else if (allyTeamCount == 2 && largestAllyTeam <= 8) {
                match.Gamemode = BarGamemode.LARGE_TEAM;
            } else if (allyTeamCount > 2 && largestAllyTeam == 1) {
                match.Gamemode = BarGamemode.FFA;
            } else if (allyTeamCount > 2 && largestAllyTeam >= 2) {
                match.Gamemode = BarGamemode.TEAM_FFA;
            } else {
                _Logger.LogWarning($"unchecked gamemode [gameID={match.ID}] [largestAllyTeam={largestAllyTeam}] [allyTeamCount={allyTeamCount}]");
            }

            _Logger.LogInformation($"demofile parsed [gameID={match.ID}] [gamemode={match.Gamemode}] [packets={packetCount}]");

            return match;
        }

    }
}
