using gex.Common.Models.Options;
using gex.Common.Services;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace gex.Tests.Services {

    [TestClass]
    public class EnginePathUtilTest {

        [TestMethod]
        public void Get_Test() {
            EnginePathUtil path = new(Options.Create(new FileStorageOptions() {
                EngineLocation = "./temp/engine",
                GameLogLocation = "",
                ReplayLocation = "",
                TempWorkLocation = "",
                WebImageLocation = ""
            }));

            string engine = "2025.04.01";
            if (OperatingSystem.IsWindows()) {
                engine += "-win";
            } else {
                engine += "-linux";
            }

            string output = path.Get("2025.04.01");
            Assert.AreEqual(Path.Join(Environment.CurrentDirectory, "temp", "engine", engine), output);
        }

    }
}
