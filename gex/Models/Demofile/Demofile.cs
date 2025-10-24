using gex.Models.Db;
using System.Collections.Generic;
using System.Text.Json;

namespace gex.Models.Demofile {

    public class Demofile {

        public DemofileHeader Header { get; set; } = new();

        public string ModConfig { get; set; } = "";

        public JsonElement HostSettings { get; set; } = new();

        public DemofileStatistics Statistics { get; set; } = new();

        public List<DemofileTeamStats> TeamStatistics { get; set; } = [];

        public List<BarMatchChatMessage> ChatMessages { get; set; } = [];

        public List<DemofileLabelPing> TextPings { get; set; } = [];

    }
}
