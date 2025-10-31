using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models;
using gex.Models.Event;
using gex.Services.Storage;
using gex.Tests.Util;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Storage {

    [TestClass]
    public class UnitPositionFileStorageTest {

        [TestCleanup]
        public void Cleanup_File() {
            File.Delete("./resources/temp/aa/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.zstd");
        }

        [TestMethod]
        public async Task Storage_RoundTrip() {
            UnitPositionFileStorage storage = new(
                logger: new TestLogger<UnitPositionFileStorage>(),
                fileOptions: Options.Create<FileStorageOptions>(new FileStorageOptions() {
                    UnitPositionLocation = "./resources/temp"
                })
            );

            string gameID = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            List<GameEventUnitPosition> positions = new([
                new GameEventUnitPosition() { GameID = gameID,
                    Frame = 0, UnitID = 1, TeamID = 2,
                    X = 50d, Y = 60d, Z = 70d
                },
                new GameEventUnitPosition() { GameID = gameID,
                    Frame = 1, UnitID = 1, TeamID = 2,
                    X = 500d, Y = 600d, Z = 700d
                },
                new GameEventUnitPosition() { GameID = gameID,
                    Frame = 2, UnitID = 1, TeamID = 2,
                    X = 5d, Y = 6d, Z = 7d
                },
                new GameEventUnitPosition() { GameID = gameID,
                    Frame = 3, UnitID = 1, TeamID = 2,
                    X = 50.5d, Y = 60.5d, Z = 70.5d
                },
                new GameEventUnitPosition() { GameID = gameID,
                    Frame = 50, UnitID = 1, TeamID = 2,
                    X = 50.55d, Y = 60.55d, Z = 70.55d
                },

            ]);

            await storage.SaveToDisk(gameID, positions, new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
            Assert.AreEqual(true, File.Exists(Path.Join("./resources/temp/aa/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.zstd")));

            Result<List<GameEventUnitPosition>, string> returned = await storage.GetByGameID(gameID, new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
            Assert.AreEqual(true, returned.IsOk, $"got error {returned.Error}");

            List<GameEventUnitPosition> pos = returned.Value;
            Assert.AreEqual(5, pos.Count);
            Assert.AreEqual(gameID, pos[0].GameID);
            Assert.AreEqual(positions[0], pos[0]);
            Assert.AreEqual(positions[1], pos[1]);
            Assert.AreEqual(positions[2], pos[2]);
            Assert.AreEqual(positions[3], pos[3]);
            Assert.AreEqual(positions[4], pos[4], $"expected {JsonSerializer.Serialize(positions[4])}\ngot {JsonSerializer.Serialize(pos[4])}");

        }

    }
}
