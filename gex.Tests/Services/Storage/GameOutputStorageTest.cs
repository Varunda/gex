using gex.Common.Models.Options;
using gex.Services.Storage;
using gex.Tests.Util;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Services.Storage {

    [TestClass]
    public class GameOutputStorageTest {

        [TestMethod]
        public void GetGameLogFolder_Test() {
            GameOutputStorage storage = new(
                logger: new TestLogger<GameOutputStorage>(),
                options: Options.Create<FileStorageOptions>(new FileStorageOptions() {
                    UnitPositionLocation = "./resources/temp",
                    GameLogLocation = "/mnt/vda1/game_logs"
                })
            );

            string gameLogLoc = storage.GetGameLogLocation("9cc46f691f6516ec84e41320251b4ae2");
            string wanted = Path.Join("/mnt/vda1/game_logs", "9c", "9cc46f691f6516ec84e41320251b4ae2");
            Assert.AreEqual(wanted, gameLogLoc);

        }


    }

}
