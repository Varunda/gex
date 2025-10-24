namespace gex.Models.Demofile {

    public class DemofileHeader {

        public string Magic { get; set; } = "";

        public int HeaderVersion { get; set; }

        public int HeaderSize { get; set; }

        public string EngineVersion { get; set; } = "";

        public string GameID { get; set; } = "";

        public long StartTime { get; set; }

        public int ScriptSize { get; set; }

        public int DemoStreamSize { get; set; }

        public int GameTime { get; set; }

        public int WallClockTime { get; set; }

        public int PlayerCount { get; set; }

        public int PlayerStatSize { get; set; }

        public int PlayerStatElemSize { get; set; }

        public int TeamCount { get; set; }

        public int TeamStatSize { get; set; }

        public int TeamStatElemSize { get; set; }

        public int TeamStatPeriod { get; set; }

        public int WinningAllyTeamsSize { get; set; }

        public int ScriptOffset => HeaderSize;

        public int PacketOffset => ScriptOffset + ScriptSize;

        public int StatOffset => PacketOffset + DemoStreamSize;

    }
}
