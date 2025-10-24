using gex.Common.Models;
using gex.Models;
using gex.Models.Bar;
using gex.Services;
using gex.Services.Parser;
using gex.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class BarWeaponDefinitionParserTest {

        private BarWeaponDefinitionParser _MakeParser() {
            LuaRunner runner = new LuaRunner(new TestLogger<LuaRunner>());

            ILogger<BarWeaponDefinitionParser> logger2 = new TestLogger<BarWeaponDefinitionParser>();
            BarWeaponDefinitionParser defParser = new(logger2, runner);

            return defParser;
        }

        private async Task<List<BarWeaponDefinition>> _Parse(string filename) {
            BarWeaponDefinitionParser parser = _MakeParser();

            string file = File.ReadAllText($"./resources/unit_data/{filename}.lua");

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            Result<List<BarWeaponDefinition>, string> res = await parser.Parse(file, cts.Token);
            Assert.IsTrue(res.IsOk, $"expected IsOk true, got error of: {res.Error}");

            return res.Value;
        }

        [TestMethod]
        public async Task Parse_Unit_Explosions() {
            BarWeaponDefinitionParser parser = _MakeParser();
            List<BarWeaponDefinition> defs = await _Parse("Unit_Explosions");

            Assert.AreEqual(207, defs.Count);

            // the ordering is not consistent
            BarWeaponDefinition? blank = defs.FirstOrDefault(iter => iter.DefinitionName == "blank");
            Assert.IsNotNull(blank);

            Assert.AreEqual("blank", blank.DefinitionName);
            Assert.AreEqual(0.123d, blank.ImpulseFactor);
            Assert.AreEqual(0d, blank.Range);

            // test one where the casing is AreaOfEffect, which is later turned into areaofeffect by the lowerkeys method
            BarWeaponDefinition? aestoreUw = defs.FirstOrDefault(iter => iter.DefinitionName == "advenergystorage-uw");
            Assert.IsNotNull(aestoreUw);
            Assert.AreEqual("advenergystorage-uw", aestoreUw.DefinitionName);
            Assert.AreEqual(480d, aestoreUw.AreaOfEffect);

            BarWeaponDefinition? mistexplo = defs.FirstOrDefault(iter => iter.DefinitionName == "mistexplo");
            Assert.IsNotNull(mistexplo);
            Assert.AreEqual("mistexplo", mistexplo.DefinitionName);
            Assert.AreEqual(true, mistexplo.IsParalyzer);
            Assert.AreEqual(20, mistexplo.ParalyzerTime);
            Assert.AreEqual(200, mistexplo.AreaOfEffect);
            Assert.AreEqual("", mistexplo.ParalyzerExceptions);

            BarWeaponDefinition? korgExplo = defs.FirstOrDefault(iter => iter.DefinitionName == "korgexplosion");
            Assert.IsNotNull(korgExplo);
            Assert.AreEqual("korgexplosion", korgExplo.DefinitionName);
            Assert.AreEqual(10600, korgExplo.Damages.GetValueOrDefault("default"));
            Assert.AreEqual(2800, korgExplo.Damages.GetValueOrDefault("commanders"));
            Assert.AreEqual(1280, korgExplo.AreaOfEffect);
        }

        [TestMethod]
        public async Task Parse_BadInputs() {
            BarWeaponDefinitionParser parser = _MakeParser();

            Result<List<BarWeaponDefinition>, string> badReturn = await parser.Parse("return 1", new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
            Assert.AreEqual(false, badReturn.IsOk);

            Result<List<BarWeaponDefinition>, string> badReturn2 = await parser.Parse("return {}", new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
            Assert.AreEqual(false, badReturn2.IsOk);

            Result<List<BarWeaponDefinition>, string> badReturn3 = await parser.Parse("return { hi = 1}", new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
            Assert.AreEqual(false, badReturn3.IsOk);
        }

    }

}
