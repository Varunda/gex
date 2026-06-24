using Dapper.ColumnMapper;
using gex.Code;
using System.Text.Json.Serialization;

namespace gex.Models.Db {

    [JsonDerivedType(typeof(BarMatchMapDrawPoint))]
    [JsonDerivedType(typeof(BarMatchMapDrawLine))]
    [JsonDerivedType(typeof(BarMatchMapDrawErase))]
    [DapperColumnsMapped]
    public class BarMatchMapDraw {

        [ColumnMapping("player_id")]
        public byte PlayerID { get; set; }

        [ColumnMapping("game_time")]
        public float GameTime { get; set; }

        [ColumnMapping("action")]
        public string Action { get; set; } = "";

        [ColumnMapping("x")]
        public int X { get; set; }

        [ColumnMapping("z")]
        public int Z { get; set; }

        [ColumnMapping("index")]
        public int Index { get; set; }

    }

    [DapperColumnsMapped]
    public class BarMatchMapDrawPoint : BarMatchMapDraw {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        [ColumnMapping("label")]
        public string Label { get; set; } = "";

        [ColumnMapping("from_lua")]
        public byte FromLua { get; set; }

    }

    public class BarMatchMapDrawLine : BarMatchMapDraw {

        public int EndX { get; set; }

        public int EndZ { get; set; }

        public byte FromLua { get; set; }

    }

    public class BarMatchMapDrawErase : BarMatchMapDraw {
        // has the same parameters as the base draw action
    }

}
