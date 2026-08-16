using gex.Common.Code.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Code.Constants {

    [TestClass]
    public class BarGamemodeTest {

        [TestMethod]
        public void GetByPlayers_Test() {

            // duel
            Assert.AreEqual(BarGamemode.DUEL, BarGamemode.GetByPlayers(2, 1));

            // small team
            Assert.AreEqual(BarGamemode.SMALL_TEAM, BarGamemode.GetByPlayers(2, 2));
            Assert.AreEqual(BarGamemode.SMALL_TEAM, BarGamemode.GetByPlayers(2, 3));
            Assert.AreEqual(BarGamemode.SMALL_TEAM, BarGamemode.GetByPlayers(2, 3));
            Assert.AreEqual(BarGamemode.SMALL_TEAM, BarGamemode.GetByPlayers(2, 4));
            Assert.AreEqual(BarGamemode.SMALL_TEAM, BarGamemode.GetByPlayers(2, 5));

            // large team
            Assert.AreEqual(BarGamemode.LARGE_TEAM, BarGamemode.GetByPlayers(2, 6));
            Assert.AreEqual(BarGamemode.LARGE_TEAM, BarGamemode.GetByPlayers(2, 7));
            Assert.AreEqual(BarGamemode.LARGE_TEAM, BarGamemode.GetByPlayers(2, 8));

            // ffa
            Assert.AreEqual(BarGamemode.FFA, BarGamemode.GetByPlayers(3, 1));
            Assert.AreEqual(BarGamemode.FFA, BarGamemode.GetByPlayers(16, 1));

            // team ffa
            Assert.AreEqual(BarGamemode.TEAM_FFA, BarGamemode.GetByPlayers(3, 2));
            Assert.AreEqual(BarGamemode.TEAM_FFA, BarGamemode.GetByPlayers(4, 2));
            Assert.AreEqual(BarGamemode.TEAM_FFA, BarGamemode.GetByPlayers(4, 4));

            // fallback
            Assert.AreEqual(BarGamemode.DEFAULT, BarGamemode.GetByPlayers(2, 9));
            Assert.AreEqual(BarGamemode.DEFAULT, BarGamemode.GetByPlayers(2, 20));
            Assert.AreEqual(BarGamemode.DEFAULT, BarGamemode.GetByPlayers(1, 1));
        }

    }
}
