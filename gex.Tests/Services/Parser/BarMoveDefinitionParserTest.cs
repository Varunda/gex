using gex.Common.Models;
using gex.Models.Bar;
using gex.Services;
using gex.Services.Parser;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class BarMoveDefinitionParserTest {

        private async Task<Result<Dictionary<string, BarMoveDefinition>, string>> _MakeParser() {
            LuaRunner runner = new(new TestLogger<LuaRunner>());
            BarMoveDefinitionParser parser = new(
                new TestLogger<BarMoveDefinitionParser>(),
                runner
            );

            string file = File.ReadAllText($"./resources/github_data/movedefs.lua");

            return await parser.GetAll(file, CancellationToken.None);
        }

        [TestMethod]
        public async Task ParseAll() {
            Result<Dictionary<string, BarMoveDefinition>, string> result = await _MakeParser();
            Assert.IsTrue(result.IsOk, $"message: {result.Error}");
        }

        [TestMethod]
        public async Task Parse_BOAT3() {
            Result<Dictionary<string, BarMoveDefinition>, string> result = await _MakeParser();
            Assert.IsTrue(result.IsOk, $"message: {result.Error}");

            BarMoveDefinition? def = result.Value.GetValueOrDefault("BOAT3");
            Assert.IsNotNull(def, "failed to find BOAT3");

            Assert.AreEqual("BOAT3", def.Name);
            Assert.AreEqual(9d, def.CrushStrength);
            Assert.AreEqual(3d, def.FootprintX);
            Assert.AreEqual(3d, def.FootprintZ);
            Assert.AreEqual(8d, def.MinWaterDepth);
        }

        [TestMethod]
        public async Task Parse_UBOAT4() {
            Result<Dictionary<string, BarMoveDefinition>, string> result = await _MakeParser();
            Assert.IsTrue(result.IsOk, $"message: {result.Error}");

            BarMoveDefinition? def = result.Value.GetValueOrDefault("UBOAT4");
            Assert.IsNotNull(def, "failed to find UBOAT4");

            Assert.AreEqual("UBOAT4", def.Name);
            Assert.AreEqual(true, def.Submarine);
        }

        [TestMethod]
        public async Task Parse_HOVER3() {
            Result<Dictionary<string, BarMoveDefinition>, string> result = await _MakeParser();
            Assert.IsTrue(result.IsOk, $"message: {result.Error}");

            BarMoveDefinition? def = result.Value.GetValueOrDefault("HOVER3");
            Assert.IsNotNull(def, "failed to find HOVER3");

            Assert.AreEqual("HOVER3", def.Name);
            Assert.AreEqual(25d, def.CrushStrength);
            Assert.AreEqual(22d, def.MaxSlope);
            Assert.AreEqual(25d, def.SlopeMod);
            Assert.AreEqual(90d, def.MaxWaterSlope);
            //Assert.AreEqual()
        }

    }
}
