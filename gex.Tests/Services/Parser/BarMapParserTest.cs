using gex.Common.Models;
using gex.Common.Models.Bar;
using gex.Common.Models.Map;
using gex.Common.Models.Options;
using gex.Common.Services;
using gex.Common.Services.Parser;
using gex.Tests.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class BarMapParserTest {

        [TestMethod]
        public async Task Test_Parse_Hooked_1_1_1() {
            ServiceCollection services = new();
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, TestLoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestLogger<>)));

            services.AddSingleton<LuaRunner>();
            services.AddSingleton<BarMapParser>();
            services.AddSingleton<BarFeatureParser>();
            services.AddSingleton<BarFeatureSetParser>();
            services.AddSingleton<IMemoryCache, NonCachingCache>();
            services.AddSingleton<IOptions<FileStorageOptions>>(Options.Create<FileStorageOptions>(new FileStorageOptions() {
                EngineLocation = "./engines/",
                MapLocation = "./maps/",
                GitHubDataLocation = "./github_data/",
            }));

            BarMapParser parser = services.BuildServiceProvider().GetRequiredService<BarMapParser>();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            Result<BarMapData, string> ret = await parser.Parse("./resources/maps/hooked_1.1.1.sd7", new BarMapParser.ParseOptions() {
                Header = true,
                HeightMap = true,
                Metal = true,
                Smts = true,
                Normals = true,
                Features = true,
            }, cts.Token);

            Assert.IsTrue(ret.IsOk, $"got parse error: {ret.Error}");

            BarMapData map = ret.Value;
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

            Assert.AreEqual(21, map.Features.Count);

            BarFeature? tree1 = map.Features.FirstOrDefault(iter => iter.Name == "ad0_senegal_1");
            Assert.IsNotNull(tree1);
            Assert.AreEqual("ad0_senegal_1", tree1.Name);
            Assert.AreEqual("Palm Tree", tree1.Description);
            Assert.AreEqual("Vegetation", tree1.Category);
            Assert.AreEqual(250, tree1.Energy);
            Assert.AreEqual(0, tree1.Metal);
            Assert.AreEqual(0.1, tree1.Damage);
            Assert.IsTrue(tree1.Flammable);
            Assert.IsTrue(tree1.Reclaimable);
            Assert.IsTrue(tree1.AutoReclaimable);
            Assert.IsTrue(tree1.Blocking);
            Assert.AreEqual(1, tree1.FootprintX);
            Assert.AreEqual(1, tree1.FootprintZ);

            Assert.IsNotNull(map.FeatureSet);
            Assert.AreEqual(1273, map.FeatureSet.ObjectList.Count);

            Assert.IsNotNull(map.MetalSpots);
            Assert.AreEqual(6, map.MetalSpots.Count);

            BarMapMetalSpot? topRight = map.MetalSpots.FirstOrDefault(iter => iter.X == 3016 && iter.Z == 40);
            Assert.IsNotNull(topRight);
            Assert.AreEqual(3016, topRight.X);
            Assert.AreEqual(40, topRight.Z);
            Assert.AreEqual(5.202d, topRight.Worth);
            Assert.AreEqual(12, topRight.SquareCount);
        }

        [TestMethod]
        public async Task Test_Parse_PillarOfDoom_V1() {
            ServiceCollection services = new();
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, TestLoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestLogger<>)));

            services.AddSingleton<LuaRunner>();
            services.AddSingleton<BarMapParser>();
            services.AddSingleton<BarFeatureParser>();
            services.AddSingleton<BarFeatureSetParser>();
            services.AddSingleton<IMemoryCache, NonCachingCache>();
            services.AddSingleton<IOptions<FileStorageOptions>>(Options.Create<FileStorageOptions>(new FileStorageOptions() {
                EngineLocation = "./engines/",
                MapLocation = "./maps/",
                GitHubDataLocation = "./github_data/",
            }));

            BarMapParser parser = services.BuildServiceProvider().GetRequiredService<BarMapParser>();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            Result<BarMapData, string> ret = await parser.Parse("./resources/maps/pillar_of_doom_v1_removed_textures.sd7", new BarMapParser.ParseOptions() {
                Header = true,
                Features = true,
            }, cts.Token);

            Assert.IsTrue(ret.IsOk, $"got parse error: {ret.Error}");

            BarMapData map = ret.Value;
            Assert.IsNotNull(map);

            Assert.IsNotNull(map.FeatureSet);
            Assert.AreEqual(342, map.FeatureSet.ObjectList.Count);
        }

        [TestMethod]
        public async Task Test_Parse_SupremeIsthmus_v2_1() {
            ServiceCollection services = new();
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, TestLoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestLogger<>)));

            services.AddSingleton<LuaRunner>();
            services.AddSingleton<BarMapParser>();
            services.AddSingleton<BarFeatureParser>();
            services.AddSingleton<BarFeatureSetParser>();
            services.AddSingleton<IMemoryCache, NonCachingCache>();
            services.AddSingleton<IOptions<FileStorageOptions>>(Options.Create<FileStorageOptions>(new FileStorageOptions() {
                EngineLocation = "./engines/",
                MapLocation = "./maps/",
                GitHubDataLocation = "./github_data/",
            }));

            BarMapParser parser = services.BuildServiceProvider().GetRequiredService<BarMapParser>();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            Result<BarMapData, string> ret = await parser.Parse("./resources/maps/supreme_isthmus_v2.1_removed_textures.sd7", new BarMapParser.ParseOptions() {
                Header = true,
                Features = true,
            }, cts.Token);

            Assert.IsTrue(ret.IsOk, $"got parse error: {ret.Error}");

            BarMapData map = ret.Value;
            Assert.IsNotNull(map);

            Assert.IsNotNull(map.FeatureSet);
            Assert.AreEqual(2642, map.FeatureSet.ObjectList.Count);
        }

    }
}
