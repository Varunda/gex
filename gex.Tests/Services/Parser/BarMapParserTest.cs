using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Services;
using gex.Common.Services.Parser;
using gex.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class BarMapParserTest {

        [TestMethod]
        public async Task Parse_Test() {
            ServiceCollection services = new();
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, TestLoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestLogger<>)));

            services.AddSingleton<LuaRunner>();
            services.AddSingleton<BarMapParser>();

            BarMapParser parser = services.BuildServiceProvider().GetRequiredService<BarMapParser>();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            Result<BarMap, string> ret = await parser.Parse("./resources/maps/hooked_1.1.1.sd7", cts.Token);

            Assert.IsTrue(ret.IsOk, $"got parse error: {ret.Error}");

            BarMap map = ret.Value;
            Assert.IsNotNull(map);

            Assert.AreEqual("Hooked 1.1.1", map.Name);
            Assert.AreEqual("1v1", map.Description);
            Assert.AreEqual(80d, map.TidalStrength);
            Assert.IsTrue(map.Author.StartsWith("Raghna "));
            Assert.AreEqual(100, map.ExtractorRadius);
            Assert.AreEqual(4, map.Height);
            Assert.AreEqual(6, map.Width);
            Assert.AreEqual(0, map.MinimumWind);
            Assert.AreEqual(8, map.MaximumWind);
            Assert.AreEqual(1.7d, map.MaxMetal);
        }

    }
}
