using gex.Common.Models;
using gex.Models.Bar;
using gex.Services.BarApi;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class BarMapRotationParserTest {

        [TestMethod]
        public void Parse_Test() {

            BarMapRotationParser parser = new(
                logger: new TestLogger<BarMapRotationParser>()
            );

            Result<List<BarMapRotation>, string> result = parser.Parse(@"
                [all]
                .*

                [certified]
                AcidicQuarry 5.17
                Aethermoor Creek 1.0
                All That Glitters Extended v1.0.2
                All That Simmers v1.1.1
                All That Smolders v1.2

                [uncertified]
                All That Glitters v2.2.3
                Blindside Remake v1.0
                Canis River v1.4
            ");

            Assert.IsTrue(result.IsOk, $"got error from parse: {result.Error}]");

            Assert.AreEqual(3, result.Value.Count);

            Assert.AreEqual("all", result.Value[0].Name);
            Assert.AreEqual(1, result.Value[0].Maps.Count);
            Assert.AreEqual(".*", result.Value[0].Maps[0]);

            Assert.AreEqual("certified", result.Value[1].Name);
            Assert.AreEqual(5, result.Value[1].Maps.Count);
            Assert.AreEqual("AcidicQuarry 5.17", result.Value[1].Maps[0]);
            Assert.AreEqual("Aethermoor Creek 1.0", result.Value[1].Maps[1]);
            Assert.AreEqual("All That Glitters Extended v1.0.2", result.Value[1].Maps[2]);
            Assert.AreEqual("All That Simmers v1.1.1", result.Value[1].Maps[3]);
            Assert.AreEqual("All That Smolders v1.2", result.Value[1].Maps[4]);

            Assert.AreEqual("uncertified", result.Value[2].Name);
            Assert.AreEqual(3, result.Value[2].Maps.Count);
            Assert.AreEqual("All That Glitters v2.2.3", result.Value[2].Maps[0]);
            Assert.AreEqual("Blindside Remake v1.0", result.Value[2].Maps[1]);
            Assert.AreEqual("Canis River v1.4", result.Value[2].Maps[2]);
        }

    }
}
