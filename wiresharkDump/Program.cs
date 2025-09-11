using System.Globalization;
using System.Text.Json;

namespace wiresharkDump {

    public class Program {

        static void Main(string[] args) {
            Program program = new(args);
            program.Run();
        }

        private string[] _Args { get; set; }

        public Program(string[] args) {
            _Args = args;
        }

        public void Run() {
            if (_Args.Length <= 1) {
                Console.WriteLine($"expected at least 2 args, got {_Args.Length} instead");
                PrintUsage();
                return;
            }

            string inputFile = Path.GetFullPath(_Args[1]);

            Console.WriteLine($"input file: {inputFile}");

            if (File.Exists(inputFile) == false) {
                Console.Error.WriteLine($"failed to find input file at {inputFile}");
                return;
            }

            using FileStream fs = File.OpenRead(inputFile);
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(fs);

            if (json.ValueKind != JsonValueKind.Array) {
                Console.Error.WriteLine($"expected input json to be an array, is a {json.ValueKind} instead");
                return;
            }

            List<WiresharkPacket> packets = new();

            foreach (JsonElement elem in json.EnumerateArray()) {
                packets.Add(WiresharkPacket.FromJson(elem));
            }

            string output = "";

            string msg = "";
            foreach (WiresharkPacket packet in packets) {

                foreach (char c in packet.DataString) {
                    if (c == '\n') {
                        output += $"{packet.SourceIp} -> {packet.DestinationIp}> {msg}\n";
                        msg = "";
                    } else {
                        msg += c;
                    }
                }
            }

            // 3 args means an output was given
            if (_Args.Length >= 3) {
                string outputFile = Path.GetFullPath(_Args[2]);
                Console.WriteLine($"output file: {outputFile}");
                File.WriteAllText(outputFile, output);
            } else {
                Console.WriteLine(output);
            }

        }

        public static void PrintUsage() {
            Console.WriteLine($"wireshark dump - take a .json wireshark dump and print out the spring lobby messages");
            Console.WriteLine($"Usage: wireshark_dump <input file> [output file]");
            Console.WriteLine($"\t<input file>\tThe input .json file to be converted into the spring lobby messages");
            Console.WriteLine($"\t<output file>\tThe output file that will be generated from the input file. if not given, printed to stdout");
        }

        private class WiresharkPacket {

            public string SourceIp { get; set; } = ""; // _source.layers.ip.src

            public string DestinationIp { get; set; } = ""; // _source.layers.ip.dst

            public byte[] Data { get; set; } = []; // _source.layers.data.data

            public string DataString {
                get {
                    return string.Join("", Data.Select(iter => (char)iter));
                }
            }

            public static WiresharkPacket FromJson(JsonElement elem) {
                WiresharkPacket packet = new();

                JsonElement ip = elem.GetProperty("_source").GetProperty("layers").GetProperty("ip");
                packet.SourceIp = ip.GetProperty("ip.src").GetString() ?? throw new Exception($"missing string for ip.src");
                packet.DestinationIp = ip.GetProperty("ip.dst").GetString() ?? throw new Exception($"missing string for ip.dst");

                string rawData = elem.GetProperty("_source").GetProperty("layers").GetProperty("data").GetProperty("data.data").GetString()
                    ?? throw new Exception($"missing data");
                string hex = string.Join("", rawData.Split(":"));

                packet.Data = ConvertHexStringToByteArray(hex);

                return packet;
            }

        }

        // https://stackoverflow.com/a/8235530
        public static byte[] ConvertHexStringToByteArray(string hexString) {
            if (hexString.Length % 2 != 0) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++) {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data; 
        }

    }
}
