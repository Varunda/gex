﻿using gex.Code.Constants;
using gex.Models.Bar;
using gex.Models.Db;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace gex.Models.Api {

    public class ApiMatch {

        public ApiMatch() {

        }

        public ApiMatch(BarMatch match) {
            ID = match.ID;
            Engine = match.Engine;
            GameVersion = match.GameVersion;
            StartTime = match.StartTime;
            Map = match.Map;
            MapName = match.MapName;
            FileName = match.FileName;
            DurationMs = match.DurationMs;
            Gamemode = match.Gamemode;
            HostSettings = match.HostSettings;
            GameSettings = match.GameSettings;
            MapSettings = match.MapSettings;
            SpadsSettings = match.SpadsSettings;
            Restrictions = match.Restrictions;
            AllyTeams = match.AllyTeams;
            Players = match.Players;
            Spectators = match.Spectators;
            ChatMessages = match.ChatMessages;
            MapData = match.MapData;
        }

        public string ID { get; set; } = "";

        public string Engine { get; set; } = "";

        public string GameVersion { get; set; } = "";

        public DateTime StartTime { get; set; }

        public string Map { get; set; } = "";

        public string MapName { get; set; } = "";

        public string FileName { get; set; } = "";

        public long DurationMs { get; set; }

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        public JsonElement HostSettings { get; set; } = default;

        public JsonElement GameSettings { get; set; } = default;

        public JsonElement MapSettings { get; set; } = default;

        public JsonElement SpadsSettings { get; set; } = default;

        public JsonElement Restrictions { get; set; } = default;

        public List<BarMatchAllyTeam> AllyTeams { get; set; } = [];

        public List<BarMatchPlayer> Players { get; set; } = [];

        public List<BarMatchSpectator> Spectators { get; set; } = [];

        public List<BarMatchChatMessage> ChatMessages { get; set; } = [];

        public BarMap? MapData { get; set; }

        public BarMatchProcessing? Processing { get; set; }

    }
}
