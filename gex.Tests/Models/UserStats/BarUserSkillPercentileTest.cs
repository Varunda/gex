using gex.Models.UserStats;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace gex.Tests.Models.UserStats {

    [TestClass]
    public class BarUserSkillPercentileTest {

        [TestMethod]
        public void Test_CalculatePercentile() {
            BarUserSkillPercentile perc = new();
            perc.P0 = 0;
            perc.P5 = 10;
            perc.P10 = 20;
            perc.P15 = 30;
            perc.P20 = 40;
            perc.P25 = 50;
            perc.P30 = 60;
            perc.P35 = 70;
            perc.P40 = 80;
            perc.P45 = 90;
            perc.P50 = 100;
            perc.P55 = 110;
            perc.P60 = 120;
            perc.P65 = 130;
            perc.P70 = 140;
            perc.P75 = 150;
            perc.P80 = 160;
            perc.P85 = 170;
            perc.P90 = 180;
            perc.P95 = 190;
            perc.P98 = 196;
            perc.P99 = 198;
            perc.P100 = 200;

            // test exact matches
            Assert.AreEqual(0, perc.CalculatePercentile(0));
            Assert.AreEqual(5, perc.CalculatePercentile(10));
            Assert.AreEqual(10, perc.CalculatePercentile(20));
            Assert.AreEqual(15, perc.CalculatePercentile(30));
            Assert.AreEqual(20, perc.CalculatePercentile(40));
            Assert.AreEqual(25, perc.CalculatePercentile(50));
            Assert.AreEqual(30, perc.CalculatePercentile(60));
            Assert.AreEqual(35, perc.CalculatePercentile(70));
            Assert.AreEqual(40, perc.CalculatePercentile(80));
            Assert.AreEqual(45, perc.CalculatePercentile(90));
            Assert.AreEqual(50, perc.CalculatePercentile(100));
            Assert.AreEqual(55, perc.CalculatePercentile(110));
            Assert.AreEqual(60, perc.CalculatePercentile(120));
            Assert.AreEqual(65, perc.CalculatePercentile(130));
            Assert.AreEqual(70, perc.CalculatePercentile(140));
            Assert.AreEqual(75, perc.CalculatePercentile(150));
            Assert.AreEqual(80, perc.CalculatePercentile(160));
            Assert.AreEqual(85, perc.CalculatePercentile(170));
            Assert.AreEqual(90, perc.CalculatePercentile(180));
            Assert.AreEqual(95, perc.CalculatePercentile(190));
            Assert.AreEqual(98, perc.CalculatePercentile(196));
            Assert.AreEqual(99, perc.CalculatePercentile(198));
            Assert.AreEqual(100, perc.CalculatePercentile(200));

            // test the 5% jumps
            Assert.AreEqual(2.5, perc.CalculatePercentile(5));
            Assert.AreEqual(7.5, perc.CalculatePercentile(15));
            Assert.AreEqual(12.5, perc.CalculatePercentile(25));
            Assert.AreEqual(17.5, perc.CalculatePercentile(35));
            Assert.AreEqual(22.5, perc.CalculatePercentile(45));
            Assert.AreEqual(27.5, perc.CalculatePercentile(55));
            Assert.AreEqual(32.5, perc.CalculatePercentile(65));
            Assert.AreEqual(37.5, perc.CalculatePercentile(75));
            Assert.AreEqual(42.5, perc.CalculatePercentile(85));
            Assert.AreEqual(47.5, perc.CalculatePercentile(95));
            Assert.AreEqual(52.5, perc.CalculatePercentile(105));
            Assert.AreEqual(57.5, perc.CalculatePercentile(115));
            Assert.AreEqual(62.5, perc.CalculatePercentile(125));
            Assert.AreEqual(67.5, perc.CalculatePercentile(135));
            Assert.AreEqual(72.5, perc.CalculatePercentile(145));
            Assert.AreEqual(77.5, perc.CalculatePercentile(155));
            Assert.AreEqual(82.5, perc.CalculatePercentile(165));
            Assert.AreEqual(87.5, perc.CalculatePercentile(175));
            Assert.AreEqual(92.5, perc.CalculatePercentile(185));
            Assert.AreEqual(97.5, perc.CalculatePercentile(195));

            // test the more precise values at the top
            Assert.AreEqual(98.5, perc.CalculatePercentile(197));
            Assert.AreEqual(99.5, perc.CalculatePercentile(199));

            for (double p = 0; p <= 100; p += 0.01) {
                Assert.AreEqual(p, perc.CalculatePercentile(p * 2d), 0.0001);
            }

        }

    }
}
