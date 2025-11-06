using System.Text.Json.Serialization;

namespace gex.Models.Db {

    [JsonDerivedType(typeof(BarMatchMapDrawPoint))]
    [JsonDerivedType(typeof(BarMatchMapDrawLine))]
    [JsonDerivedType(typeof(BarMatchMapDrawErase))]
    public class BarMatchMapDraw {

        public byte PlayerID { get; set; }

        public float GameTime { get; set; }

        public string Action { get; set; } = "";

        public int X { get; set; }

        public int Z { get; set; }

    }

    public class BarMatchMapDrawPoint : BarMatchMapDraw {

        public string Label { get; set; } = "";

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
