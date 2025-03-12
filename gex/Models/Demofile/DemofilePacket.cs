namespace gex.Models.Demofile {

    public class DemofilePacket {

        public float GameTime { get; set; }

        public uint Length { get; set; }

        public byte PacketType { get; set; }

        public byte[] Data { get; set; } = [];

    }
}
