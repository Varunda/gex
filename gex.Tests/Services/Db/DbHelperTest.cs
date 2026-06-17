using DotNet.Testcontainers.Builders;
using gex.Models.Options;
using gex.Services.Db;
using gex.Services.Db.Implementations;
using gex.Tests.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace gex.Tests.Services.Db {

    [TestClass]
    public class DbHelperTest {

        [TestMethod]
        public async Task Test_DbInitialize() {
            await DbUtil.Create();
        }
        
    }
}
