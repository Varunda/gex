using gex.Common.Models;
using gex.Models.Map;
using gex.Services.Parser;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class StartSpotPositionsParserTest {

        [TestMethod]
        public void Parse_Test() {

            JsonElement json = JsonSerializer.Deserialize<JsonElement>("{\"team\":[" +
                "{\"sides\":[{\"starts\":[{\"role\":\"front\",\"spawnPoint\":\"P1\"},{\"role\":\"front/tech\",\"spawnPoint\":\"P2\"}," +
                "{\"role\":\"front\",\"spawnPoint\":\"P3\"},{\"role\":\"front\",\"baseCenter\":\"P17\",\"spawnPoint\":\"P4\"}," +
                "{\"role\":\"air\",\"spawnPoint\":\"P5\"},{\"role\":\"front\",\"spawnPoint\":\"P6\"},{\"role\":\"front/tech\",\"spawnPoint\":\"P7\"}," +
                "{\"role\":\"front\",\"spawnPoint\":\"P8\"}]},{\"starts\":[{\"role\":\"front\",\"spawnPoint\":\"P9\"}," +
                "{\"role\":\"front/tech\",\"spawnPoint\":\"P10\"},{\"role\":\"front\",\"spawnPoint\":\"P11\"}," +
                "{\"role\":\"front\",\"baseCenter\":\"P18\",\"spawnPoint\":\"P12\"},{\"role\":\"air\",\"spawnPoint\":\"P13\"}," +
                "{\"role\":\"front\",\"spawnPoint\":\"P14\"},{\"role\":\"front/tech\",\"spawnPoint\":\"P15\"},{\"role\":\"front\",\"spawnPoint\":\"P16\"}]}]," +
                "\"teamCount\":2,\"playersPerTeam\":8}]," +
                "\"positions\":{\"P1\":{\"x\":2077,\"y\":231},\"P2\":{\"x\":550,\"y\":985},\"P3\":{\"x\":2053,\"y\":2680},\"P4\":{\"x\":2079,\"y\":3753}," +
                "\"P5\":{\"x\":420,\"y\":3773},\"P6\":{\"x\":2089,\"y\":5028},\"P7\":{\"x\":642,\"y\":6582},\"P8\":{\"x\":1959,\"y\":7408}," +
                "\"P9\":{\"x\":8135,\"y\":7911},\"P10\":{\"x\":9652,\"y\":7075},\"P11\":{\"x\":8175,\"y\":5413},\"P12\":{\"x\":8157,\"y\":4352}," +
                "\"P13\":{\"x\":9831,\"y\":4337},\"P14\":{\"x\":8187,\"y\":3135},\"P15\":{\"x\":9627,\"y\":1521},\"P16\":{\"x\":8339,\"y\":708}," +
                "\"P17\":{\"x\":3584,\"y\":3804},\"P18\":{\"x\":6650,\"y\":4301}}}");

            StartSpotDataParser parser = new(new TestLogger<StartSpotDataParser>());

            Result<StartSpotData, string> res = parser.Parse("roseta_v1.4", json);
            Assert.IsTrue(res.IsOk, $"got error from parse: {res.Error}");

            StartSpotData pos = res.Value;
            Assert.IsTrue(JsonElement.DeepEquals(json, pos.Raw));

            Assert.AreEqual("roseta_v1.4", pos.MapFilename);

            Assert.AreEqual(18, pos.Positions.Count, $"expected 18 positions, got {pos.Positions.Count} instead");
            Assert.AreEqual("roseta_v1.4", pos.Positions[0].MapFilename);
            Assert.AreEqual("P1", pos.Positions[0].Name);
            Assert.AreEqual(2077f, pos.Positions[0].X);
            Assert.AreEqual(231f, pos.Positions[0].Y);

            Assert.AreEqual("roseta_v1.4", pos.Positions[17].MapFilename);
            Assert.AreEqual("P9", pos.Positions[17].Name);
            Assert.AreEqual(8135f, pos.Positions[17].X);
            Assert.AreEqual(7911f, pos.Positions[17].Y);

            Assert.AreEqual("roseta_v1.4", pos.Positions[7].MapFilename);
            Assert.AreEqual("P16", pos.Positions[7].Name);
            Assert.AreEqual(8339f, pos.Positions[7].X);
            Assert.AreEqual(708f, pos.Positions[7].Y);

            Assert.AreEqual(1, pos.Configurations.Count);

            StartSpotConfiguration config = pos.Configurations[0];
            Assert.AreEqual("roseta_v1.4", config.MapFilename);
            Assert.AreEqual(8, config.PlayersPerTeam);
            Assert.AreEqual(2, config.TeamCount);
            Assert.AreEqual(2, config.Sides.Count);

            StartSpotSide side = config.Sides[0];
            Assert.AreEqual("roseta_v1.4", side.MapFilename);
            Assert.AreEqual(0, side.Index);
            Assert.AreEqual(2, side.TeamCount);
            Assert.AreEqual(8, side.PlayersPerTeam);
            Assert.AreEqual(8, side.Starts.Count);

            Assert.AreEqual("front", side.Starts[0].Role);
            Assert.AreEqual("front", side.Starts[0].BaseRole);
            Assert.AreEqual("P1", side.Starts[0].SpawnPoint);
            Assert.AreEqual(0, side.Starts[0].SideIndex);
            Assert.IsNull(side.Starts[0].BaseCenter);

            Assert.AreEqual("front", side.Starts[3].Role);
            Assert.AreEqual("front", side.Starts[3].BaseRole);
            Assert.AreEqual(0, side.Starts[3].SideIndex);
            Assert.AreEqual("P4", side.Starts[3].SpawnPoint);
            Assert.AreEqual("P17", side.Starts[3].BaseCenter);

        }

    }
}
