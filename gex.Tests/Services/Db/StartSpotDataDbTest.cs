using gex.Models.Map;
using gex.Services.Db;
using gex.Services.Db.Map;
using gex.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Db {

    [TestClass]
    public class StartSpotDataDbTest {

        [TestMethod]
        public async Task Test_InsertAndGet() {

            IDbHelper helper = await DbUtil.Create();

            StartSpotDataDb dut = new(new LoggerFactory(), helper);

            StartSpotData data = new();
            data.MapFilename = "rosetta_v1.4";
            data.Version = 4;

            await dut.Insert(data, CancellationToken.None);

            List<StartSpotData> found = await dut.GetByVersionAndMapFilename("rosetta_v1.4", 4, CancellationToken.None);
            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(4, found[0].Version);
            Assert.AreEqual("rosetta_v1.4", found[0].MapFilename);
        }

    }
}
