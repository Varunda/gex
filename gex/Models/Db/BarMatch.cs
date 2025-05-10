using gex.Code.Constants;
using gex.Models.Bar;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace gex.Models.Db {

    public class BarMatch {

        public string ID { get; set; } = "";

        public string Engine { get; set; } = "";

        public string GameVersion { get; set; } = "";

        public DateTime StartTime { get; set; }

        public string Map { get; set; } = "";

        public string MapName { get; set; } = "";

        public string FileName { get; set; } = "";

        public long DurationMs { get; set; }

		/// <summary>
		///		how many frames long this game is. IS NOT 100% ACCURATE, as it only updates on key frames
		/// </summary>
		public long DurationFrameCount { get; set; }

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        public JsonElement HostSettings { get; set; } = default;

        public JsonElement GameSettings { get; set; } = default;

        public JsonElement MapSettings { get; set; } = default;

        public JsonElement SpadsSettings { get; set; } = default;

        public JsonElement Restrictions { get; set; } = default;

        public List<BarMatchAllyTeam> AllyTeams { get; set; } = [];

        public List<BarMatchPlayer> Players { get; set; } = [];

        public List<BarMatchSpectator> Spectators { get; set; } = [];

		/// <summary>
		///		not currently saved in the DB, only returned from parsing
		/// </summary>
		public List<BarMatchAiPlayer> AiPlayers { get; set; } = [];

        public List<BarMatchChatMessage> ChatMessages { get; set; } = [];

		public int PlayerCount { get; set; }

		public long? UploadedBy { get; set; }

        public BarMap? MapData { get; set; }

    }
}
