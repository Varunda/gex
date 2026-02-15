using gex.Common.Models;
using gex.Common.Services.Bar;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class TdfParserTest {

        [TestMethod]
        public void Parse_Test() {

            TdfParser parser = new(new TestLogger<TdfParser>());

            Result<JsonElement, string> result = parser.Parse("[game] { a = 4 }");
            Assert.IsTrue(result.IsOk);
        }

    }
}
