using gex.Common.Models;
using gex.Models.Map;
using gex.Services.Parser;
using gex.Tests.Util;
using Lua.CodeAnalysis.Compilation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Tests.Models.Map {

    [TestClass]
    public class StartSpotDataTest {

        [TestMethod]
        public void Test_Equality() {

            Assert.AreEqual((StartSpotData?)null, (StartSpotData?)null);
            Assert.AreNotEqual(new StartSpotData(), (StartSpotData?)null);
            Assert.AreNotEqual((StartSpotData?)null, new StartSpotData());

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

            Result<StartSpotData, string> res1 = parser.Parse("roseta_v1.4", json);
            Assert.IsTrue(res1.IsOk, $"got error from parse: {res1.Error}");

            StartSpotData s1 = res1.Value;

            Func<StartSpotData> GetS2 = () => {
                Result<StartSpotData, string> res2 = parser.Parse("roseta_v1.4", json);
                Assert.IsTrue(res2.IsOk, $"got error from parse: {res2.Error}");
                Assert.AreEqual(s1, res2.Value);
                return res2.Value;
            };

            StartSpotData s2 = GetS2();

            Assert.AreEqual(s1, s2);

            s1.Version = 1;
            s2.Version = 2;
            Assert.AreEqual(s1, s2);

            s2.MapFilename = "not rosetta";
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Positions.Clear();
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();
            Assert.AreEqual(s1, s2);

            /*
            s2.Positions[0].Name = "wrong";
            s2.Raw = JsonSerializer.SerializeToElement(s2);
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Positions[0].X = 0;
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Positions[0].Y = 0;
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Positions[0].Version = 4;
            Assert.AreEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].PlayersPerTeam = 1;
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].TeamCount = 3;
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].Version = 4;
            Assert.AreEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].Sides[0].Index = 1;
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].Sides[0].Starts[0].Role = "wrong";
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].Sides[0].Starts[0].SpawnPoint = "wrong";
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();

            s2.Configurations[0].Sides[0].Starts[0].BaseCenter = "wrong";
            Assert.AreNotEqual(s1, s2);
            s2 = GetS2();
            */
        }

    }
}
