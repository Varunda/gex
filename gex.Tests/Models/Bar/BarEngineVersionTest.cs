using gex.Models.Bar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Models.Bar {

    [TestClass]
    public class BarEngineVersionTest {

        /// <summary>
        ///     test parsing the older BAR105 format works and gets the correct version numbers
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="version"></param>
        [DataTestMethod]
        [DataRow("105.1.1-1821-gaca6f20 BAR105", 1821)]
        [DataRow("105.1.1-2590-gb9462a0 BAR105", 2590)]
        public void Test_Format_BAR105_Parsing(string engine, int version) {
            BarEngineVersion ev = new(engine);
            Assert.AreEqual(BarEngineVersion.FORMAT_BAR105, ev.Format);
            Assert.AreEqual(version, ev.Version);
        }

        /// <summary>
        ///     get the YYYY.MM.build format works and gets the correct version numbers
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="version"></param>
        [DataTestMethod]
        [DataRow("2025.04.08", 20250408)]
        [DataRow("2025.01.06", 20250106)]
        [DataRow("2025.10.01", 20251001)]
        [DataRow("2025.99.99", 20259999)]
        public void Test_Format_YM_Parsing(string engine, int version) {
            BarEngineVersion ev = new(engine);
            Assert.AreEqual(BarEngineVersion.FORMAT_YM, ev.Format);
            Assert.AreEqual(version, ev.Version);
        }

        /// <summary>
        ///     test comparisions between engine versions
        /// </summary>
        [TestMethod]
        public void Test_Comparisons() {
            BarEngineVersion a = new("2025.01.06");
            BarEngineVersion b = new("2025.01.07");
            BarEngineVersion c = new("105.1.1-1821-gaca6f20 BAR105");
            BarEngineVersion d = new("105.1.1-2590-gb9462a0 BAR105");

            Assert.IsTrue(a.Equals(a));
#pragma warning disable CS1718 // Comparison made to same variable, yeah that's intentional
            Assert.IsTrue(a == a);
            Assert.IsTrue(a <= a);
            Assert.IsTrue(a >= a);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.IsTrue(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != d);
            Assert.IsTrue(a < b);
            Assert.IsTrue(a <= b);
            Assert.IsTrue(a > c);
            Assert.IsTrue(a >= c);
            Assert.IsTrue(a > d);
            Assert.IsTrue(a >= d);

            Assert.IsTrue(b != c);
            Assert.IsTrue(b != d);
            Assert.IsTrue(b > c);
            Assert.IsTrue(b >= c);
            Assert.IsTrue(b > d);
            Assert.IsTrue(b >= d);

            Assert.IsTrue(c != d);
            Assert.IsTrue(c < d);
            Assert.IsTrue(c <= d);
        }

    }
}
