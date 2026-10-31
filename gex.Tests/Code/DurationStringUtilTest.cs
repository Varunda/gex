using gex.Common.Code;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace gex.Tests.Code {

    [TestClass]
    public class DurationStringUtilTest {

        /// <summary>
        ///     valid input tests
        /// </summary>
        [TestMethod]
        public void Test_Inputs() {
            static void _t(string input, int days, int hours, int minutes, int seconds) {
                TimeSpan output1 = DurationStringUtil.Parse(input);
                Assert.AreEqual(days, output1.Days);
                Assert.AreEqual(hours, output1.Hours);
                Assert.AreEqual(minutes, output1.Minutes);
                Assert.AreEqual(seconds, output1.Seconds);
            }

            _t("1h40m", 0, 1, 40, 0); // basic
            _t("1H40M", 0, 1, 40, 0); // case-insensitive
            _t("67D21H41M12S", 67, 21, 41, 12);
            _t("61324D0H59M59S", 61324, 0, 59, 59);
            _t("120s", 0, 0, 2, 0); // test component overflow (120 seconds => 2 minutes)
        }

        /// <summary>
        ///     ensure giving repeat components (such as 2 day components) throws
        /// </summary>
        [TestMethod]
        public void Test_BadInput_RepeatComponent() {
            Assert.ThrowsException<ArgumentException>(() => { DurationStringUtil.Parse("1d1d"); });
            Assert.ThrowsException<ArgumentException>(() => { DurationStringUtil.Parse("1h1h"); });
            Assert.ThrowsException<ArgumentException>(() => { DurationStringUtil.Parse("1m1m"); });
            Assert.ThrowsException<ArgumentException>(() => { DurationStringUtil.Parse("1s1s"); });
        }

        /// <summary>
        ///     ensure invalid characters throw
        /// </summary>
        [TestMethod]
        public void Test_BadInput_InvalidCharacters() {
            // can't express negative components like this
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("-4h"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("4d 4s"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("\n4d"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(" 4d"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("-4h "); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("1.2d"); });
        }

        /// <summary>
        ///     ensure empty or whitespace inputs throw
        /// </summary>
        [TestMethod]
        public void Test_BadInput_EmptyInput() {
            // empty inputs
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(""); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(" "); });

            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("\t"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(" \t"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("\t "); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(" \t "); });

            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("\n"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(" \n"); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse("\n "); });
            Assert.ThrowsException<Exception>(() => { DurationStringUtil.Parse(" \n "); });
        }

    }
}
