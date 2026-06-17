using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Common.Services;
using gex.Models.Bar;
using gex.Services.Parser;
using gex.Services.Repositories;
using gex.Services.Util;
using gex.Tests.Util;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Util {

    [TestClass]
    public class MapSymmetryUtilTest {

        [TestMethod]
        public async Task Parse() {

            IOptions<FileStorageOptions> fileStorageOptions = Options.Create(
                new FileStorageOptions() {
                    TempWorkLocation = "./temp",
                    WebImageLocation = "./images"
                }
            );

            MapSymmetryUtil util = new(
                logger: new TestLogger<MapSymmetryUtil>(),
                mapImageRepository: new MapImageRepository(new TestLogger<MapImageRepository>(), fileStorageOptions),
                fileStorageOptions
            );

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            Result<MapSymmetryAxis, string> hellas = await util.Find("hellas_basin_v1.4");
            Assert.IsTrue(hellas.IsOk, $"output not ok: {hellas.Error}");
            Assert.AreEqual(MapSymmetryAxis.MIRRORED_DIAGONAL, hellas.Value);

            Result<MapSymmetryAxis, string> glitters = await util.Find("all_that_glitters_v2.2.3");
            Assert.IsTrue(glitters.IsOk, $"output not ok: {glitters.Error}");
            Assert.AreEqual(MapSymmetryAxis.FLIPPED_HORIZONTAL, glitters.Value);

        }

    }
}
