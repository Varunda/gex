using gex.Common.Models;
using gex.Services;
using gex.Services.Parser;
using gex.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
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
    public class BarIconTypeParserTest {

        [TestMethod]
        public async Task Test_Parse() {
            BarIconTypeParser dut = new BarIconTypeParser(
                logger: new TestLogger<BarIconTypeParser>(),
                luaRunner: new LuaRunner(new TestLogger<LuaRunner>())
            );

            string lua = await File.ReadAllTextAsync("./resources/github_data/icontypes.lua");

            Result<Dictionary<string, string>, string> result = await dut.Parse(lua, CancellationToken.None);
            Assert.IsTrue(result.IsOk, $"error: {result.Error}");

            Dictionary<string, string> dict = result.Value;
            Assert.AreEqual("factory_bot_t2.png", dict.GetValueOrDefault("corhalab"));
            Assert.IsNull(dict.GetValueOrDefault("armah_scav"));
            Assert.AreEqual("armpwt4.png", dict.GetValueOrDefault("squadarmpwt4"));
            Assert.AreEqual("sub_rez.png", dict.GetValueOrDefault("armrecl"));
            Assert.AreEqual("radar_t1.png", dict.GetValueOrDefault("armrad"));
        }

    }
}
