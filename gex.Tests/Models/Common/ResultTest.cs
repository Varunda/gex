using gex.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Models.Common {

    [TestClass]
    public class ResultTest {

        [TestMethod]
        public void Test_NullableOk() {
            Result<string?, string> a = Result<string?, string>.Ok(null);
            Assert.IsTrue(a.IsOk);
            Assert.IsNull(a.Value);

            Result<List<int>?, string> b = Result<List<int>?, string>.Ok(null);
            Assert.IsTrue(b.IsOk);
            Assert.IsNull(b.Value);

            Result<int?, string> c = Result<int?, string>.Ok(null);
            Assert.IsTrue(c.IsOk);
            Assert.IsNull(c.Value);
        }

    }
}
